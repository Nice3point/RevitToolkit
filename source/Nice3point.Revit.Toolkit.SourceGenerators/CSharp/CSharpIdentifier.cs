using Microsoft.CodeAnalysis.CSharp;

namespace Nice3point.Revit.Toolkit.SourceGenerators.CSharp;

internal static class CSharpIdentifier
{
    public static string Escape(string name)
    {
        return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None
            ? $"@{name}"
            : name;
    }

    public static string Reserve(string name, HashSet<string> usedNames)
    {
        var candidate = name;
        var suffix = 1;
        while (!usedNames.Add(candidate))
        {
            candidate = $"{name}{suffix++}";
        }

        return candidate;
    }
}
