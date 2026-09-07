using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests.Verifiers;

internal sealed class AnalyzerTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public AnalyzerTest()
    {
        ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        CompilerDiagnostics = CompilerDiagnostics.Errors;
        TestState.Sources.Add(("Toolkit.cs", TestSources.ExternalEventAttribute));
        SolutionTransforms.Add(static (solution, projectId) => solution.WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.CSharp14)));
    }
}
