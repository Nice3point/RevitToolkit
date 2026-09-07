using Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests;

[Category("Regression")]
public sealed class GeneratorHintNameTests
{
    [Test]
    [Arguments("Consumer", "Model", "Consumer", "model")]
    [Arguments("Consumer", "Model", "consumer", "Model")]
    public async Task CaseSensitiveDeclarations_ProduceDistinctHintNamesAsync(string firstNamespace, string firstType, string secondNamespace, string secondType)
    {
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;
                       namespace {{firstNamespace}}
                       {
                           public partial class {{firstType}}
                           {
                               [ExternalEvent] private void Run() { }
                           }
                       }
                       namespace {{secondNamespace}}
                       {
                           public partial class {{secondType}}
                           {
                               [ExternalEvent] private void Run() { }
                           }
                       }
                       """;

        var (driver, input) = await GeneratorTest.RunAsync(source);
        var sources = driver.GetRunResult().Results.Single().GeneratedSources;

        await GeneratorTest.AssertCompilesAsync(input);
        await Assert.That(sources).Count().IsEqualTo(2);
        await Assert.That(sources.Select(static result => result.HintName).Distinct(StringComparer.OrdinalIgnoreCase)).Count().IsEqualTo(2);
    }
}
