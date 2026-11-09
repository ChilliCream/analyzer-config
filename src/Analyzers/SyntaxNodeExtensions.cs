using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ChilliCream.Analyzers;

internal static class SyntaxNodeExtensions
{
    extension(SyntaxNode node)
    {
        public CrefSyntax? GetTopCref()
        {
            return node.AncestorsAndSelf().OfType<CrefSyntax>().LastOrDefault();
        }
    }
}
