using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ChilliCream.Analyzers;

internal static class SyntaxNodeExtensions
{
    extension(SyntaxNode node)
    {
        public CrefSyntax? GetTopCref()
        {
            return node.AncestorsAndSelf().OfType<CrefSyntax>().LastOrDefault();
        }

        public bool HasComment(TextSpan span)
        {
            foreach (var trivia in node.DescendantTrivia(span))
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

        public bool HasDirective(TextSpan span)
        {
            foreach (var trivia in node.DescendantTrivia(span))
            {
                if (trivia.IsDirective)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
