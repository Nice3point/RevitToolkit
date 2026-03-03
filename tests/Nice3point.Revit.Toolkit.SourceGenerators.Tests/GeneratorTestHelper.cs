using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests;

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

            public interface IAsyncExternalEvent
            {
                System.Threading.Tasks.Task RaiseAsync();
            }

            public interface IAsyncExternalEvent<T>
            {
                System.Threading.Tasks.Task<T> RaiseAsync();
            }

            public class ExternalEvent : ExternalEventHandler, IExternalEvent
            {
                public ExternalEvent(System.Action handler) { }
                public ExternalEvent(System.Action handler, ExternalEventOptions options) { }
                public ExternalEvent(System.Action<Autodesk.Revit.UI.UIApplication> handler) { }
                public ExternalEvent(System.Action<Autodesk.Revit.UI.UIApplication> handler, ExternalEventOptions options) { }
                public override void Execute(Autodesk.Revit.UI.UIApplication uiApplication) { }
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
                public AsyncExternalEvent(System.Func<T> handler) { }
                public AsyncExternalEvent(System.Func<T> handler, ExternalEventOptions options) { }
                public AsyncExternalEvent(System.Func<Autodesk.Revit.UI.UIApplication, T> handler) { }
                public AsyncExternalEvent(System.Func<Autodesk.Revit.UI.UIApplication, T> handler, ExternalEventOptions options) { }
                public override void Execute(Autodesk.Revit.UI.UIApplication uiApplication) { }
                public System.Threading.Tasks.Task<T> RaiseAsync() => System.Threading.Tasks.Task.FromResult<T>(default!);
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
