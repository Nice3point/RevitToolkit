using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Nice3point.Revit.Toolkit.Analyzers.ExternalEvents;
using FixTest = Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests.Verifiers.CodeFixTest<
    Nice3point.Revit.Toolkit.Analyzers.AsyncVoidMethodAnalyzer,
    Nice3point.Revit.Toolkit.Analyzers.CodeFixers.CodeFixes.RemoveAsyncModifierCodeFixer>;

namespace Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests;

public sealed class RemoveAsyncModifierCodeFixerTests
{
    [Test]
    public async Task AsyncWithoutAwait_RemovesModifierAsync()
    {
        var test = new FixTest
        {
            TestCode = """
                       using Nice3point.Revit.Toolkit.External;

                       public partial class Model
                       {
                           [ExternalEvent]
                           private async void {|#0:Run|}() { }
                       }
                       """,
            FixedCode = """
                        using Nice3point.Revit.Toolkit.External;

                        public partial class Model
                        {
                            [ExternalEvent]
                            private void Run() { }
                        }
                        """
        };

        test.ExpectedDiagnostics.Add(AsyncVoidDiagnostic(0, "Run"));
        await test.RunAsync();
    }

    [Test]
    [Arguments("async System.Threading.Tasks.Task NestedAsync() { await System.Threading.Tasks.Task.Yield(); }\n        _ = NestedAsync();")]
    [Arguments("System.Func<System.Threading.Tasks.Task> nested = async () => await System.Threading.Tasks.Task.Yield();\n        _ = nested();")]
    [Arguments("System.Func<System.Threading.Tasks.Task> nested = async delegate { await System.Threading.Tasks.Task.Yield(); };\n        _ = nested();")]
    public async Task NestedAsyncFunction_PreservesInnerAwaitAsync([StringSyntax("C#")] string body)
    {
        var test = new FixTest
        {
            TestCode = $$"""
                         using Nice3point.Revit.Toolkit.External;

                         public partial class Model
                         {
                             [ExternalEvent]
                             private async void {|#0:Run|}()
                             {
                                 {{body}}
                             }
                         }
                         """,
            FixedCode = $$"""
                          using Nice3point.Revit.Toolkit.External;

                          public partial class Model
                          {
                              [ExternalEvent]
                              private void Run()
                              {
                                  {{body}}
                              }
                          }
                          """
        };

        test.ExpectedDiagnostics.Add(AsyncVoidDiagnostic(0, "Run"));
        await test.RunAsync();
    }

    [Test]
    [Category("Regression")]
    [Arguments("await System.Threading.Tasks.Task.Yield();")]
    [Arguments("await using var stream = new System.IO.MemoryStream();")]
    [Arguments("await using (var stream = new System.IO.MemoryStream()) { }")]
    [Arguments("await foreach (var item in System.Threading.Channels.Channel.CreateUnbounded<int>().Reader.ReadAllAsync()) { }")]
    public async Task AsyncWithAwait_OffersNoUnsafeFixAsync([StringSyntax("C#")] string body)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;

                       public partial class Model
                       {
                           [ExternalEvent]
                           private async void {|#0:Run|}()
                           {
                               {{body}}
                           }
                       }
                       """;

        var test = new FixTest
        {
            TestCode = source,
            FixedCode = source,
            NumberOfIncrementalIterations = 0,
            NumberOfFixAllIterations = 0
        };

        test.FixedState.ExpectedDiagnostics.Add(AsyncVoidDiagnostic(0, "Run"));
        test.ExpectedDiagnostics.Add(AsyncVoidDiagnostic(0, "Run"));
        await test.RunAsync();
    }

    [Test]
    [Category("Regression")]
    public async Task AsyncModifierComment_PreservesCommentAsync()
    {
        var test = new FixTest
        {
            TestCode = """
                       using Nice3point.Revit.Toolkit.External;

                       public partial class Model
                       {
                           [ExternalEvent]
                           private async /* Revit entry point. */ void {|#0:Run|}() { }
                       }
                       """,
            FixedCode = """
                        using Nice3point.Revit.Toolkit.External;

                        public partial class Model
                        {
                            [ExternalEvent]
                            private /* Revit entry point. */ void Run() { }
                        }
                        """
        };

        test.ExpectedDiagnostics.Add(AsyncVoidDiagnostic(0, "Run"));
        await test.RunAsync();
    }

    [Test]
    public async Task FixAll_RemovesSafeAsyncModifiersAsync()
    {
        var test = new FixTest
        {
            TestCode = """
                       using Nice3point.Revit.Toolkit.External;

                       public partial class Model
                       {
                           [ExternalEvent]
                           private async void {|#0:First|}() { }

                           [ExternalEvent]
                           private async void {|#1:Second|}() { }
                       }
                       """,
            FixedCode = """
                        using Nice3point.Revit.Toolkit.External;

                        public partial class Model
                        {
                            [ExternalEvent]
                            private void First() { }

                            [ExternalEvent]
                            private void Second() { }
                        }
                        """,
            NumberOfIncrementalIterations = 2,
            NumberOfFixAllIterations = 1
        };

        test.ExpectedDiagnostics.AddRange([
            AsyncVoidDiagnostic(0, "First"),
            AsyncVoidDiagnostic(1, "Second")
        ]);

        await test.RunAsync();
    }

    [Test]
    public async Task FixAll_PreservesMethodsRequiringAwaitAsync()
    {
        var test = new FixTest
        {
            TestCode = """
                       using Nice3point.Revit.Toolkit.External;

                       public partial class Model
                       {
                           [ExternalEvent]
                           private async void {|#0:First|}() { }

                           [ExternalEvent]
                           private async void {|#1:Second|}() => await System.Threading.Tasks.Task.Yield();
                       }
                       """,
            FixedCode = """
                        using Nice3point.Revit.Toolkit.External;

                        public partial class Model
                        {
                            [ExternalEvent]
                            private void First() { }

                            [ExternalEvent]
                            private async void {|#1:Second|}() => await System.Threading.Tasks.Task.Yield();
                        }
                        """,
            NumberOfIncrementalIterations = 1,
            NumberOfFixAllIterations = 1
        };

        test.ExpectedDiagnostics.AddRange([
            AsyncVoidDiagnostic(0, "First"),
            AsyncVoidDiagnostic(1, "Second")
        ]);

        test.FixedState.ExpectedDiagnostics.Add(AsyncVoidDiagnostic(1, "Second"));
        await test.RunAsync();
    }

    private static DiagnosticResult AsyncVoidDiagnostic(int location, string methodName)
    {
        return new DiagnosticResult(ExternalEventDiagnostics.AsyncVoidMethod)
            .WithLocation(location)
            .WithArguments(methodName);
    }
}
