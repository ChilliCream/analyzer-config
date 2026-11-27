using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ChilliCream.Analyzers;

internal readonly struct SpeculativeRewrite(
    ExpressionSyntax node,
    SyntaxNode target,
    SyntaxNode replacement,
    ISymbol symbol)
{
    public ExpressionSyntax Node { get; } = node;

    public SyntaxNode Replacement { get; } = replacement;

    public ISymbol Symbol { get; } = symbol;

    public SyntaxNode Target { get; } = target;
}
