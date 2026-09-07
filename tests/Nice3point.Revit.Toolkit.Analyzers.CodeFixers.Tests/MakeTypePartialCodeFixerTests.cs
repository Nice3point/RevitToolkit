using Microsoft.CodeAnalysis.Testing;
using Nice3point.Revit.Toolkit.Analyzers.ExternalEvents;
using FixTest = Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests.Verifiers.CodeFixTest<
    Nice3point.Revit.Toolkit.Analyzers.ExternalEventContainingTypeNotPartialAnalyzer,
    Nice3point.Revit.Toolkit.Analyzers.CodeFixers.CodeFixes.MakeTypePartialCodeFixer>;

namespace Nice3point.Revit.Toolkit.Analyzers.CodeFixers.Tests;

public sealed class MakeTypePartialCodeFixerTests
{
    [Test]
    [Arguments("class")]
    [Arguments("struct")]
    [Arguments("record")]
    [Arguments("record struct")]
    public async Task NonPartialType_AddsModifierAsync(string typeKind)
    {
        var test = new FixTest
        {
            TestCode = $$"""
                         using Nice3point.Revit.Toolkit.External;

                         public {{typeKind}} {|#0:Model|}
                         {
                             [ExternalEvent]
                             private void Run() { }
                         }
                         """,
            FixedCode = $$"""
                          using Nice3point.Revit.Toolkit.External;

                          public partial {{typeKind}} Model
                          {
                              [ExternalEvent]
                              private void Run() { }
                          }
                          """
        };

        test.ExpectedDiagnostics.Add(PartialDiagnostic(0, "Model"));
        await test.RunAsync();
    }

    [Test]
    public async Task GenericType_PreservesConstraintsAsync()
    {
        var test = new FixTest
        {
            TestCode = """
                       using Nice3point.Revit.Toolkit.External;

                       public class {|#0:Model|}<T> where T : class, new()
                       {
                           [ExternalEvent]
                           private void Run(T value) { }
                       }
                       """,
            FixedCode = """
                        using Nice3point.Revit.Toolkit.External;

                        public partial class Model<T> where T : class, new()
                        {
                            [ExternalEvent]
                            private void Run(T value) { }
                        }
                        """
        };

        test.ExpectedDiagnostics.Add(PartialDiagnostic(0, "Model"));
        await test.RunAsync();
    }

    [Test]
    public async Task NonPartialOuterType_AddsModifierAsync()
    {
        var test = new FixTest
        {
            TestCode = """
                       using Nice3point.Revit.Toolkit.External;

                       public class {|#0:Outer|}
                       {
                           public partial class Inner
                           {
                               [ExternalEvent]
                               private void Run() { }
                           }
                       }
                       """,
            FixedCode = """
                        using Nice3point.Revit.Toolkit.External;

                        public partial class Outer
                        {
                            public partial class Inner
                            {
                                [ExternalEvent]
                                private void Run() { }
                            }
                        }
                        """
        };

        test.ExpectedDiagnostics.Add(PartialDiagnostic(0, "Outer"));
        await test.RunAsync();
    }

    [Test]
    [Category("Regression")]
    [Arguments("// Revit command model.", "")]
    [Arguments("#region Models", "\n#endregion")]
    public async Task LeadingTrivia_PreservesPositionAsync(string leadingTrivia, string trailingTrivia)
    {
        var test = new FixTest
        {
            TestCode = $$"""
                         using Nice3point.Revit.Toolkit.External;

                         {{leadingTrivia}}
                         class {|#0:Model|}
                         {
                             [ExternalEvent]
                             private void Run() { }
                         }{{trailingTrivia}}
                         """,
            FixedCode = $$"""
                          using Nice3point.Revit.Toolkit.External;

                          {{leadingTrivia}}
                          partial class Model
                          {
                              [ExternalEvent]
                              private void Run() { }
                          }{{trailingTrivia}}
                          """
        };

        test.ExpectedDiagnostics.Add(PartialDiagnostic(0, "Model"));
        await test.RunAsync();
    }

    [Test]
    public async Task FixAll_MakesContainingTypesPartialAsync()
    {
        var test = new FixTest
        {
            TestCode = """
                       using Nice3point.Revit.Toolkit.External;

                       public class {|#0:Outer|}
                       {
                           public class {|#1:Inner|}
                           {
                               [ExternalEvent]
                               private void Run() { }
                           }
                       }
                       """,
            FixedCode = """
                        using Nice3point.Revit.Toolkit.External;

                        public partial class Outer
                        {
                            public partial class Inner
                            {
                                [ExternalEvent]
                                private void Run() { }
                            }
                        }
                        """,
            NumberOfIncrementalIterations = 2,
            NumberOfFixAllIterations = 1
        };

        test.ExpectedDiagnostics.AddRange([
            PartialDiagnostic(0, "Outer"),
            PartialDiagnostic(1, "Inner")
        ]);

        await test.RunAsync();
    }

    private static DiagnosticResult PartialDiagnostic(int location, string typeName)
    {
        return new DiagnosticResult(ExternalEventDiagnostics.ContainingTypeNotPartial)
            .WithLocation(location)
            .WithArguments(typeName, "Run");
    }
}
