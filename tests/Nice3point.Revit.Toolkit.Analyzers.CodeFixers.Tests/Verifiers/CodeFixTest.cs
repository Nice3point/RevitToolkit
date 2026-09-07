using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests.Verifiers;

internal sealed class CodeFixTest<TAnalyzer, TCodeFix> : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public CodeFixTest()
    {
        ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        CompilerDiagnostics = CompilerDiagnostics.Errors;
        TestState.Sources.Add(("Toolkit.cs", TestSources.ExternalEventAttribute));
        FixedState.Sources.Add(("Toolkit.cs", TestSources.ExternalEventAttribute));
        SolutionTransforms.Add(static (solution, projectId) => solution.WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.CSharp14)));
    }
}
