using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nice3point.Revit.Toolkit.Analyzers.ExternalEvents;

namespace Nice3point.Revit.Toolkit.Analyzers.CodeFixers.CodeFixes;

/// <summary>
///     A code fixer that adds the <see langword="partial" /> modifier to a type declaration
///     when a method marked with <c>[ExternalEvent]</c> is inside a non-partial type.
/// </summary>
[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class MakeTypePartialCodeFixer : CodeFixProvider
{
    private const string Title = "Make type partial";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
    [
        ExternalEventDiagnostics.ContainingTypeNotPartial.Id
    ];

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = context.Span;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var typeDeclaration = root!.FindNode(diagnosticSpan).FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (typeDeclaration is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                _ => AddPartialModifierAsync(context.Document, root, typeDeclaration),
                Title),
            diagnostic);
    }

    /// <summary>
    ///     Adds the <see langword="partial" /> modifier to the target type declaration.
    /// </summary>
    private static Task<Document> AddPartialModifierAsync(Document document, SyntaxNode root, TypeDeclarationSyntax typeDeclaration)
    {
        var partialKeyword = SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space);
        var declaration = typeDeclaration.WithoutLeadingTrivia();
        var newTypeDeclaration = declaration.WithModifiers(declaration.Modifiers.Add(partialKeyword))
            .WithLeadingTrivia(typeDeclaration.GetLeadingTrivia());

        var newRoot = root.ReplaceNode(typeDeclaration, newTypeDeclaration);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
