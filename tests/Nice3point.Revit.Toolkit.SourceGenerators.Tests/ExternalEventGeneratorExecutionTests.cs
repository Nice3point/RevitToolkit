using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests;

[Category("Regression")]
public sealed class ExternalEventGeneratorExecutionTests
{
    [Test]
    [Arguments("class", "", LanguageVersion.CSharp13, false)]
    [Arguments("class", "", LanguageVersion.CSharp14, false)]
    [Arguments("class", "static ", LanguageVersion.CSharp13, false)]
    [Arguments("class", "static ", LanguageVersion.CSharp14, false)]
    [Arguments("struct", "", LanguageVersion.CSharp13, false)]
    [Arguments("struct", "", LanguageVersion.CSharp14, false)]
    [Arguments("struct", "static ", LanguageVersion.CSharp13, false)]
    [Arguments("struct", "static ", LanguageVersion.CSharp14, false)]
    [Arguments("class", "", LanguageVersion.CSharp13, true)]
    [Arguments("class", "", LanguageVersion.CSharp14, true)]
    [Arguments("class", "static ", LanguageVersion.CSharp14, true)]
    [Arguments("struct", "", LanguageVersion.CSharp14, true)]
    [Arguments("struct", "static ", LanguageVersion.CSharp14, true)]
    public async Task ConcurrentFirstAccess_ConstructsOneEventPerPropertyAsync(string typeKind, string modifier, LanguageVersion languageVersion, bool useLockType)
    {
        var receiver = modifier.Length == 0 ? "model" : "Model";
        var source = $$"""
                       using System;
                       using System.Linq;
                       using System.Threading;
                       using System.Threading.Tasks;
                       using Nice3point.Revit.Toolkit.External;

                       namespace Consumer;

                       public partial {{typeKind}} Model
                       {
                           [ExternalEvent]
                           private {{modifier}}void Run(int first, int second) { }
                       }

                       public static class Probe
                       {
                           public static async Task<int[]> RunAsync()
                           {
                               var model = new Model();
                               var constructions = 0;
                               EventConstruction.Observer = () => Interlocked.Increment(ref constructions);
                               var sync = await ReadConcurrentlyAsync(() => {{receiver}}.RunEvent);
                               var syncConstructions = constructions;
                               var asyncEvents = await ReadConcurrentlyAsync(() => {{receiver}}.RunAsyncEvent);
                               return new[]
                               {
                                   syncConstructions,
                                   constructions - syncConstructions,
                                   sync.All(item => ReferenceEquals(item, {{receiver}}.RunEvent)) ? 1 : 0,
                                   asyncEvents.All(item => ReferenceEquals(item, {{receiver}}.RunAsyncEvent)) ? 1 : 0
                               };
                           }

                           private static async Task<object[]> ReadConcurrentlyAsync(Func<object> read)
                           {
                               const int count = 8;
                               using var barrier = new Barrier(count);
                               var readers = Enumerable.Range(0, count).Select(_ => Task.Factory.StartNew(() =>
                               {
                                   if (!barrier.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
                                   return read();
                               }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
                               return await Task.WhenAll(readers);
                           }
                       }
                       """;

        var (driver, input) = await GeneratorTest.RunAsync(source, languageVersion: languageVersion,
            referenceAssemblies: useLockType ? ReferenceAssemblies.Net.Net90 : ReferenceAssemblies.Net.Net80);
        var result = await GeneratorTest.ExecuteAsync<int[]>(driver, input);

        await Assert.That(result).IsEquivalentTo([1, 1, 1, 1]);
    }

    [Test]
    [Arguments("class", LanguageVersion.CSharp13, false)]
    [Arguments("class", LanguageVersion.CSharp14, false)]
    [Arguments("struct", LanguageVersion.CSharp13, false)]
    [Arguments("struct", LanguageVersion.CSharp14, false)]
    [Arguments("class", LanguageVersion.CSharp13, true)]
    [Arguments("class", LanguageVersion.CSharp14, true)]
    [Arguments("struct", LanguageVersion.CSharp14, true)]
    public async Task FailedInitialization_RetriesAndInvokesCapturedHandlerAsync(string typeKind, LanguageVersion languageVersion, bool useLockType)
    {
        var source = $$"""
                       using System;
                       using System.Threading.Tasks;
                       using Nice3point.Revit.Toolkit.External;

                       namespace Consumer;

                       public partial {{typeKind}} Model
                       {
                           public int Offset;
                           [ExternalEvent]
                           private int handler(int args, int application) => Offset + args * 10 + application;
                       }

                       public static class Probe
                       {
                           public static async Task<int[]> RunAsync()
                           {
                               var attempts = 0;
                               EventConstruction.Observer = () =>
                               {
                                   if (++attempts == 1) throw new InvalidOperationException("First construction");
                               };
                               var model = new Model { Offset = 100 };
                               var failures = 0;
                               try { _ = model.handlerAsyncEvent; }
                               catch (InvalidOperationException) { failures++; }
                               model.Offset = 150;
                               var externalEvent = model.handlerAsyncEvent;
                               model.Offset = 200;
                               var result = await externalEvent.RaiseAsync(args: 2, application: 7);
                               return new[] { failures, attempts, result, ReferenceEquals(externalEvent, model.handlerAsyncEvent) ? 1 : 0 };
                           }
                       }
                       """;

        var (driver, input) = await GeneratorTest.RunAsync(source, languageVersion: languageVersion,
            referenceAssemblies: useLockType ? ReferenceAssemblies.Net.Net90 : ReferenceAssemblies.Net.Net80);
        var result = await GeneratorTest.ExecuteAsync<int[]>(driver, input);

        await Assert.That(result[0]).IsEqualTo(1);
        await Assert.That(result[1]).IsEqualTo(2);
        await Assert.That(result[2]).IsEqualTo(typeKind == "struct" ? 177 : 227);
        await Assert.That(result[3]).IsEqualTo(1);
    }

    [Test]
    public async Task SeparateInstances_RetainIndependentHandlersAsync()
    {
        const string source = """
                              using System.Threading.Tasks;
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer;

                              public partial class Model(int offset)
                              {
                                  [ExternalEvent]
                                  private int Run(int value) => offset + value;
                              }

                              public static class Probe
                              {
                                  public static async Task<int[]> RunAsync()
                                  {
                                      var first = new Model(10);
                                      var second = new Model(20);
                                      return new[]
                                      {
                                          await first.RunAsyncEvent.RaiseAsync(1),
                                          await second.RunAsyncEvent.RaiseAsync(2),
                                          ReferenceEquals(first.RunAsyncEvent, second.RunAsyncEvent) ? 1 : 0
                                      };
                                  }
                              }
                              """;

        var (driver, input) = await GeneratorTest.RunAsync(source);
        var result = await GeneratorTest.ExecuteAsync<int[]>(driver, input);

        await Assert.That(result[0]).IsEqualTo(11);
        await Assert.That(result[1]).IsEqualTo(22);
        await Assert.That(result[2]).IsZero();
    }
}
