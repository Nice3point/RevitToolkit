using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nice3point.Revit.Toolkit.Analyzers.Diagnostics;

namespace Nice3point.Revit.Toolkit.Analyzers.CodeFixers.CodeFixes;

/// <summary>
///     A code fixer that removes the <see langword="async" /> modifier from an <see langword="async" /> <see langword="void" /> method
///     marked with <c>[ExternalEvent]</c>.
/// </summary>
[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class RemoveAsyncModifierCodeFixer : CodeFixProvider
{
    private const string Title = "Remove async modifier";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [DiagnosticDescriptors.ExternalEventAsyncVoidMethod.Id];

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = context.Span;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var methodDeclaration = root!.FindNode(diagnosticSpan).FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (methodDeclaration is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                _ => RemoveAsyncModifier(context.Document, root, methodDeclaration),
                Title),
            diagnostic);
    }

    /// <summary>
    ///     Removes the <see langword="async" /> modifier from the method declaration.
    /// </summary>
    private static Task<Document> RemoveAsyncModifier(Document document, SyntaxNode root, MethodDeclarationSyntax methodDeclaration)
    {
        var asyncToken = methodDeclaration.Modifiers.First(modifier => modifier.IsKind(SyntaxKind.AsyncKeyword));
        var newModifiers = methodDeclaration.Modifiers.Remove(asyncToken);
        var newMethodDeclaration = methodDeclaration.WithModifiers(newModifiers);

        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(methodDeclaration, newMethodDeclaration)));
    }
}
