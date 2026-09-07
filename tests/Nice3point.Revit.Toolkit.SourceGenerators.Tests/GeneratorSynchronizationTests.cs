using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests;

public sealed class GeneratorSynchronizationTests
{
    [Test]
    [Arguments(LanguageVersion.CSharp12, false, "object")]
    [Arguments(LanguageVersion.CSharp13, false, "object")]
    [Arguments(LanguageVersion.CSharp14, false, "object")]
    [Arguments(LanguageVersion.CSharp12, true, "object")]
    [Arguments(LanguageVersion.CSharp13, true, "global::System.Threading.Lock")]
    [Arguments(LanguageVersion.CSharp14, true, "global::System.Threading.Lock")]
    public async Task LockType_RequiresLanguageAndFrameworkSupportAsync(LanguageVersion languageVersion, bool hasLockType, string expectedType)
    {
        const string source = """
                              using Nice3point.Revit.Toolkit.External;

                              namespace Consumer
                              {
                                  public partial class Model
                                  {
                                      [ExternalEvent]
                                      private int Run(int first, int second) => first + second;
                                  }
                              }

                              namespace Consumer.System { public class Threading { } }
                              """;

        var (driver, _) = await GeneratorTest.RunAsync(source, languageVersion: languageVersion, referenceAssemblies: hasLockType ? ReferenceAssemblies.Net.Net90 : ReferenceAssemblies.Net.Net80);
        var generatedTree = driver.GetRunResult().GeneratedTrees.Single();
        var declarations = generatedTree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>();
        var gate = declarations.Single(declaration => declaration.Declaration.Variables.Single().Identifier.ValueText == "_RunAsyncEventLock");

        await Assert.That(gate.Declaration.Type.ToString()).IsEqualTo($"{expectedType}?");
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task MultiParameterHandler_UsesBlockLambdaAsync(bool returnsValue)
    {
        var returnType = returnsValue ? "int" : "void";
        var body = returnsValue ? "return first + second;" : string.Empty;
        var source = $$"""
                       using Nice3point.Revit.Toolkit.External;
                       public partial class Model
                       {
                           [ExternalEvent(AllowDirectInvocation = true)]
                           private {{returnType}} Run(int first, int second) { {{body}} }
                       }
                       """;

        var (driver, _) = await GeneratorTest.RunAsync(source);
        var lambdas = driver.GetRunResult().GeneratedTrees.Single().GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToArray();

        await Assert.That(lambdas.Length).IsEqualTo(returnsValue ? 1 : 2);
        foreach (var lambda in lambdas)
        {
            await Assert.That(lambda.Block).IsNotNull();
            var statement = lambda.Block!.Statements.Single();
            await Assert.That(statement.ToString()).IsEqualTo(returnsValue ? "return Run(args.First, args.Second);" : "Run(args.First, args.Second);");
        }
    }
}
