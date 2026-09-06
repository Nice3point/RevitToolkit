using Nice3point.Revit.Toolkit.Analyzers.Diagnostics;
using Verifier = Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests.Verifiers.CSharpCodeFixVerifier<
    Nice3point.Revit.Toolkit.Analyzers.ExternalEventContainingTypeNotPartialAnalyzer,
    Nice3point.Revit.Toolkit.Analyzers.CodeFixers.CodeFixes.MakeTypePartialCodeFixer>;

namespace Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests;

public sealed class MakeTypePartialCodeFixerTests
{
    [Test]
    public async Task NonPartialClass_ReportsDiagnosticAndFixes()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            namespace Nice3point.Revit.Toolkit.External
            {
                public sealed class ExternalEventAttribute : System.Attribute { }
            }

            namespace TestApplication
            {
                public class {|#0:MyViewModel|}
                {
                    [Nice3point.Revit.Toolkit.External.ExternalEvent]
                    private void DoWork() { }
                }
            }
            """,
            Verifier.Diagnostic(DiagnosticDescriptors.ExternalEventContainingTypeNotPartial)
                .WithLocation(0)
                .WithArguments("MyViewModel", "DoWork"),
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
    public async Task PartialClass_NoDiagnostic()
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
    public async Task MethodWithoutAttribute_NoDiagnostic()
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
                    private void DoWork() { }
                }
            }
            """);
    }

    [Test]
    public async Task NestedNonPartialClass_ReportsDiagnosticAndFixes()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            namespace Nice3point.Revit.Toolkit.External
            {
                public sealed class ExternalEventAttribute : System.Attribute { }
            }

            namespace TestApplication
            {
                public class {|#0:Outer|}
                {
                    public partial class Inner
                    {
                        [Nice3point.Revit.Toolkit.External.ExternalEvent]
                        private void DoWork() { }
                    }
                }
            }
            """,
            Verifier.Diagnostic(DiagnosticDescriptors.ExternalEventContainingTypeNotPartial)
                .WithLocation(0)
                .WithArguments("Outer", "DoWork"),
            """
            namespace Nice3point.Revit.Toolkit.External
            {
                public sealed class ExternalEventAttribute : System.Attribute { }
            }

            namespace TestApplication
            {
                public partial class Outer
                {
                    public partial class Inner
                    {
                        [Nice3point.Revit.Toolkit.External.ExternalEvent]
                        private void DoWork() { }
                    }
                }
            }
            """);
    }
}
