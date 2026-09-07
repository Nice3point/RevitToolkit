using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Nice3point.Revit.Toolkit.Analyzers.ExternalEvents;

namespace Nice3point.Revit.Toolkit.Analyzers.CodeFixers.CodeFixes;

/// <summary>
///     A code fixer that removes the <see langword="async" /> modifier from an <see langword="async" /> <see langword="void" /> method
///     marked with <c>[ExternalEvent]</c>.
/// </summary>
/// <remarks>A fix is available only when the method body contains no await operations outside nested functions.</remarks>
[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class RemoveAsyncModifierCodeFixer : CodeFixProvider
{
    private const string Title = "Remove async modifier";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
    [
        ExternalEventDiagnostics.AsyncVoidMethod.Id
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
        var methodDeclaration = root!.FindNode(diagnosticSpan).FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (methodDeclaration is null)
        {
            return;
        }

        foreach (var node in methodDeclaration.DescendantNodes(static node => node is not (LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)))
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (node is AwaitExpressionSyntax
                or UsingStatementSyntax { AwaitKeyword.RawKind: not 0 }
                or LocalDeclarationStatementSyntax { AwaitKeyword.RawKind: not 0 }
                or CommonForEachStatementSyntax { AwaitKeyword.RawKind: not 0 })
            {
                return;
            }
        }

        var asyncToken = methodDeclaration.Modifiers.First(static modifier => modifier.IsKind(SyntaxKind.AsyncKeyword));
        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                cancellationToken => RemoveAsyncModifierAsync(context.Document, asyncToken, cancellationToken),
                Title),
            diagnostic);
    }

    /// <summary>
    ///     Removes the <see langword="async" /> modifier from the method declaration.
    /// </summary>
    private static async Task<Document> RemoveAsyncModifierAsync(Document document, SyntaxToken asyncToken, CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var trailingTrivia = asyncToken.TrailingTrivia.Count > 0 ? asyncToken.TrailingTrivia[0] : default;
        var end = trailingTrivia.IsKind(SyntaxKind.WhitespaceTrivia) ? trailingTrivia.Span.End : asyncToken.Span.End;
        var span = TextSpan.FromBounds(asyncToken.SpanStart, end);

        return document.WithText(text.WithChanges(new TextChange(span, string.Empty)));
    }
}
