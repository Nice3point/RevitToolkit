using Nice3point.Revit.Toolkit.Analyzers.Diagnostics;
using Verifier = Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests.Verifiers.CSharpCodeFixVerifier<
    Nice3point.Revit.Toolkit.Analyzers.AsyncVoidMethodAnalyzer,
    Nice3point.Revit.Toolkit.Analyzers.CodeFixers.CodeFixes.RemoveAsyncModifierCodeFixer>;

namespace Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests;

public sealed class RemoveAsyncModifierCodeFixerTests
{
    [Test]
    public async Task AsyncVoidMethod_ReportsDiagnosticAndFixes()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            namespace Nice3point.Revit.Toolkit.External
            {
                public sealed class ExternalEventAttribute : System.Attribute { }
            }

            namespace TestApplication
            {
                public partial class MyViewModel
                {
                    [Nice3point.Revit.Toolkit.External.ExternalEvent]
                    private async void {|#0:DoWork|}() { }
                }
            }
            """,
            Verifier.Diagnostic(DiagnosticDescriptors.ExternalEventAsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("DoWork"),
            """
            using System.Threading.Tasks;

            namespace Nice3point.Revit.Toolkit.External
            {
                public sealed class ExternalEventAttribute : System.Attribute { }
            }

            namespace TestApplication
            {
                public partial class MyViewModel
                {
                    [Nice3point.Revit.Toolkit.External.ExternalEvent]
                    private void DoWork() { }
                }
            }
            """);
    }

    [Test]
    public async Task NonAsyncVoidMethod_NoDiagnostic()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            namespace Nice3point.Revit.Toolkit.External
            {
                public sealed class ExternalEventAttribute : System.Attribute { }
            }

            namespace TestApplication
            {
                public partial class MyViewModel
                {
                    [Nice3point.Revit.Toolkit.External.ExternalEvent]
                    private void DoWork() { }
                }
            }
            """);
    }

    [Test]
    public async Task AsyncVoidWithoutAttribute_NoDiagnostic()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            namespace Nice3point.Revit.Toolkit.External
            {
                public sealed class ExternalEventAttribute : System.Attribute { }
            }

            namespace TestApplication
            {
                public class MyViewModel
                {
                    private async void DoWork() { }
                }
            }
            """);
    }
}
