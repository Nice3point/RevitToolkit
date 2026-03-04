using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Nice3point.Revit.Toolkit.Analyzers.Diagnostics;

namespace Nice3point.Revit.Toolkit.Analyzers.CodeFixers.CodeFixes;

/// <summary>
///     A code fixer that changes the return type of an <see langword="async"/> <see langword="void"/> method
///     marked with <c>[ExternalEvent]</c> to return <see cref="Task"/> instead.
/// </summary>
[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class ChangeAsyncVoidToTaskCodeFixer : CodeFixProvider
{
    private const string Title = "Change return type to Task";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [DiagnosticDescriptors.MethodIsAsyncVoid.Id];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = context.Span;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        if (root.FindNode(diagnosticSpan).FirstAncestorOrSelf<MethodDeclarationSyntax>() is { } methodDeclaration)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: token => ChangeReturnType(context.Document, root, methodDeclaration, token),
                    equivalenceKey: Title),
                diagnostic);
        }
    }

    /// <summary>
    ///     Changes the return type of the method from <see langword="void"/> to <see cref="Task"/>.
    /// </summary>
    private static async Task<Document> ChangeReturnType(Document document, SyntaxNode root, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
    {
        if (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } semanticModel)
        {
            return document;
        }

        if (semanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task") is not { } taskSymbol)
        {
            return document;
        }

        var typeSyntax = SyntaxGenerator.GetGenerator(document)
            .TypeExpression(taskSymbol)
            .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);

        return document.WithSyntaxRoot(root.ReplaceNode(methodDeclaration.ReturnType, typeSyntax));
    }
}