using System.Diagnostics.CodeAnalysis;

namespace Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests.Verifiers;

internal static class TestSources
{
    [StringSyntax("C#")] public const string ExternalEventAttribute =
        """
        namespace Nice3point.Revit.Toolkit.External;

        [System.AttributeUsage(System.AttributeTargets.Method)]
        public sealed class ExternalEventAttribute : System.Attribute;
        """;
}
