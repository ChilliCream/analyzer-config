using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ChilliCream.Analyzers;

public sealed partial class UseDeclaringTypeAnalyzer
{
    private static bool HasComment(ExpressionSyntax qualifier)
    {
        foreach (var trivia in qualifier.DescendantTrivia(qualifier.Span))
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsColorColorForm(
        SemanticModel semanticModel,
        int position,
        ExpressionSyntax form,
        ISymbol? receiver,
        INamedTypeSymbol declaringType,
        CancellationToken cancellationToken)
    {
        return form is SimpleNameSyntax
            && SymbolEqualityComparer.Default.Equals(
                receiver.GetReceiverType(position, cancellationToken),
                declaringType)
            && semanticModel.BindsAsType(position, form.ToString(), declaringType);
    }

    private static bool IsInsideNameof(
        SemanticModel semanticModel,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        foreach (var ancestor in node.Ancestors())
        {
            if (ancestor is InvocationExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.Text: "nameof" }
                } invocation
                && invocation.ArgumentList.Span.Contains(node.Span)
                && semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is null)
            {
                return true;
            }
        }

        return false;
    }

    private static bool KeepsBinding(
        SemanticModel semanticModel,
        Candidate candidate,
        CancellationToken cancellationToken)
    {
        if (candidate.Node is not ExpressionSyntax node
            || SpeculativeBinding.GetRoot(node) is not { } root)
        {
            return false;
        }

        var replacement = SyntaxFactory
            .ParseExpression(candidate.Replacement)
            .WithTriviaFrom(candidate.Qualifier);
        List<SpeculativeRewrite> rewrites =
        [
            new(node, candidate.Qualifier, replacement, candidate.Member)
        ];

        SpeculativeBinding.DropRewritesThatChangeBinding(
            semanticModel,
            root,
            rewrites,
            cancellationToken);

        return rewrites.Count == 1;
    }
}
