using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests.Verifiers;

internal static class CSharpVerifierHelper
{
    /// <summary>
    ///     Compiler diagnostic IDs related to nullability mapped to <see cref="ReportDiagnostic.Error"/>,
    ///     used to enable all nullable warnings for default validation during analyzer and code fix tests.
    /// </summary>
    internal static ImmutableDictionary<string, ReportDiagnostic> NullableWarnings { get; } = GetNullableWarningsFromCompiler();

    private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
    {
        string[] args = ["/warnaserror:nullable", "-p:LangVersion=preview"];
        var commandLineArguments = CSharpCommandLineParser.Default.Parse(args, baseDirectory: Environment.CurrentDirectory, sdkDirectory: Environment.CurrentDirectory);
        var nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;

        nullableWarnings = nullableWarnings
            .SetItem("CS8632", ReportDiagnostic.Error)
            .SetItem("CS8669", ReportDiagnostic.Error)
            .SetItem("CS8652", ReportDiagnostic.Suppress);

        return nullableWarnings;
    }
}
