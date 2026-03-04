using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests.Verifiers;

public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static DiagnosticResult Diagnostic() => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic();
    public static DiagnosticResult Diagnostic(string diagnosticId) => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(diagnosticId);
    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor) => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(descriptor);

    /// <summary>
    ///     Verifies that the analyzer produces the expected diagnostics on the given source.
    /// </summary>
    public static async Task VerifyAnalyzerAsync(
        [StringSyntax("c#-test")] string source,
        params DiagnosticResult[] expected)
    {
        var test = new Test
        {
            TestCode = source,
            CompilerDiagnostics = CompilerDiagnostics.None
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }

    public static async Task VerifyCodeFixAsync(
        [StringSyntax("c#-test")] string source,
        [StringSyntax("c#-test")] string fixedSource)
    {
        await VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);
    }

    public static async Task VerifyCodeFixAsync(
        [StringSyntax("c#-test")] string source,
        DiagnosticResult expected,
        [StringSyntax("c#-test")] string fixedSource)
    {
        await VerifyCodeFixAsync(source, [expected], fixedSource);
    }

    public static async Task VerifyCodeFixAsync(
        [StringSyntax("c#-test")] string source,
        IEnumerable<DiagnosticResult> expected,
        [StringSyntax("c#-test")] string fixedSource)
    {
        var test = new Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            CompilerDiagnostics = CompilerDiagnostics.None,
            CodeFixTestBehaviors = CodeFixTestBehaviors.SkipLocalDiagnosticCheck
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }
}