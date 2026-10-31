using Microsoft.CodeAnalysis.CSharp;
using Nice3point.Revit.Toolkit.SourceGenerators.CSharp;
using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators.AddInAssemblies;

internal static class AddInAssemblyWriter
{
    public const string HintName = "AddInAssemblies.g.cs";

    public static string GenerateSource(EquatableArray<string> assemblyNames)
    {
        var writer = new CodeWriter();
        foreach (var assemblyName in assemblyNames)
        {
            writer.AppendLine($"[assembly: {AddInProjectReferences.AddInAssemblyAttribute}({SymbolDisplay.FormatLiteral(assemblyName, quote: true)})]");
        }

        return writer.ToString();
    }
}
