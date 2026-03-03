using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;

public static class GeneratorTestHelper
{
    private const string StubTypes = """
        namespace Autodesk.Revit.UI
        {
            public class UIApplication { }
            public enum ExternalEventRequest { Accepted, Denied, TimedOut }
            public interface IExternalEventHandler
            {
                void Execute(UIApplication uiApplication);
                string GetName();
            }
        }

        namespace Nice3point.Revit.Toolkit.External
        {
            [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
            public sealed class ExternalEventAttribute : System.Attribute
            {
                public bool AllowDirectInvocation { get; set; }
            }

            [System.Flags]
            public enum ExternalEventOptions
            {
                None = 0,
                AllowDirectInvocation = 1 << 0
            }

            public abstract class ExternalEventHandler : Autodesk.Revit.UI.IExternalEventHandler
            {
                public abstract void Execute(Autodesk.Revit.UI.UIApplication uiApplication);
                public virtual string GetName() => GetType().Name;
                public virtual Autodesk.Revit.UI.ExternalEventRequest Raise() => default;
            }

            public interface IExternalEvent
            {
                Autodesk.Revit.UI.ExternalEventRequest Raise();
            }

            public interface IExternalEvent<T>
            {
                Autodesk.Revit.UI.ExternalEventRequest Raise(T args);
            }

            public interface IAsyncExternalEvent
            {
                System.Threading.Tasks.Task RaiseAsync();
            }

            public interface IAsyncExternalEvent<T>
            {
                System.Threading.Tasks.Task RaiseAsync(T args);
            }

            public interface IAsyncRequestExternalEvent<TResult>
            {
                System.Threading.Tasks.Task<TResult> RaiseAsync();
            }

            public interface IAsyncRequestExternalEvent<T, TResult>
            {
                System.Threading.Tasks.Task<TResult> RaiseAsync(T args);
            }

            public class ExternalEvent : ExternalEventHandler, IExternalEvent
            {
                public ExternalEvent(System.Action handler) { }
                public ExternalEvent(System.Action handler, ExternalEventOptions options) { }
                public ExternalEvent(System.Action<Autodesk.Revit.UI.UIApplication> handler) { }
                public ExternalEvent(System.Action<Autodesk.Revit.UI.UIApplication> handler, ExternalEventOptions options) { }
                public override void Execute(Autodesk.Revit.UI.UIApplication uiApplication) { }
            }

            public class ExternalEvent<T> : ExternalEventHandler, IExternalEvent<T>
            {
                public ExternalEvent(System.Action<T> handler) { }
                public ExternalEvent(System.Action<T> handler, ExternalEventOptions options) { }
                public ExternalEvent(System.Action<Autodesk.Revit.UI.UIApplication, T> handler) { }
                public ExternalEvent(System.Action<Autodesk.Revit.UI.UIApplication, T> handler, ExternalEventOptions options) { }
                public override void Execute(Autodesk.Revit.UI.UIApplication uiApplication) { }
                public Autodesk.Revit.UI.ExternalEventRequest Raise(T args) => default;
            }

            public sealed class AsyncExternalEvent : ExternalEventHandler, IAsyncExternalEvent
            {
                public AsyncExternalEvent(System.Action handler) { }
                public AsyncExternalEvent(System.Action handler, ExternalEventOptions options) { }
                public AsyncExternalEvent(System.Action<Autodesk.Revit.UI.UIApplication> handler) { }
                public AsyncExternalEvent(System.Action<Autodesk.Revit.UI.UIApplication> handler, ExternalEventOptions options) { }
                public override void Execute(Autodesk.Revit.UI.UIApplication uiApplication) { }
                public System.Threading.Tasks.Task RaiseAsync() => System.Threading.Tasks.Task.CompletedTask;
            }

            public sealed class AsyncExternalEvent<T> : ExternalEventHandler, IAsyncExternalEvent<T>
            {
                public AsyncExternalEvent(System.Action<T> handler) { }
                public AsyncExternalEvent(System.Action<T> handler, ExternalEventOptions options) { }
                public AsyncExternalEvent(System.Action<Autodesk.Revit.UI.UIApplication, T> handler) { }
                public AsyncExternalEvent(System.Action<Autodesk.Revit.UI.UIApplication, T> handler, ExternalEventOptions options) { }
                public override void Execute(Autodesk.Revit.UI.UIApplication uiApplication) { }
                public System.Threading.Tasks.Task RaiseAsync(T args) => System.Threading.Tasks.Task.CompletedTask;
            }

            public sealed class AsyncRequestExternalEvent<TResult> : ExternalEventHandler, IAsyncRequestExternalEvent<TResult>
            {
                public AsyncRequestExternalEvent(System.Func<TResult> handler) { }
                public AsyncRequestExternalEvent(System.Func<TResult> handler, ExternalEventOptions options) { }
                public AsyncRequestExternalEvent(System.Func<Autodesk.Revit.UI.UIApplication, TResult> handler) { }
                public AsyncRequestExternalEvent(System.Func<Autodesk.Revit.UI.UIApplication, TResult> handler, ExternalEventOptions options) { }
                public override void Execute(Autodesk.Revit.UI.UIApplication uiApplication) { }
                public System.Threading.Tasks.Task<TResult> RaiseAsync() => System.Threading.Tasks.Task.FromResult<TResult>(default!);
            }

            public sealed class AsyncRequestExternalEvent<T, TResult> : ExternalEventHandler, IAsyncRequestExternalEvent<T, TResult>
            {
                public AsyncRequestExternalEvent(System.Func<T, TResult> handler) { }
                public AsyncRequestExternalEvent(System.Func<T, TResult> handler, ExternalEventOptions options) { }
                public AsyncRequestExternalEvent(System.Func<Autodesk.Revit.UI.UIApplication, T, TResult> handler) { }
                public AsyncRequestExternalEvent(System.Func<Autodesk.Revit.UI.UIApplication, T, TResult> handler, ExternalEventOptions options) { }
                public override void Execute(Autodesk.Revit.UI.UIApplication uiApplication) { }
                public System.Threading.Tasks.Task<TResult> RaiseAsync(T args) => System.Threading.Tasks.Task.FromResult<TResult>(default!);
            }

            public static class RevitContext
            {
                public static Autodesk.Revit.UI.UIApplication UiApplication => null!;
                public static bool IsRevitInApiMode => false;
            }
        }
        """;

    public static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator(string source)
    {
        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(StubTypes),
            CSharpSyntaxTree.ParseText(source)
        };

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>()
            .ToList();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ExternalEventGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedSources = runResult.GeneratedTrees
            .Select(syntaxTree => syntaxTree.GetText().ToString())
            .ToArray();

        return (diagnostics, generatedSources);
    }
}
