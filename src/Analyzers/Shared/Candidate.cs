using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ChilliCream.Analyzers;

internal readonly struct Candidate(
    SyntaxNode node,
    ExpressionSyntax qualifier,
    ISymbol member,
    string replacement)
{
    public SyntaxNode Node { get; } = node;

    public ExpressionSyntax Qualifier { get; } = qualifier;

    public ISymbol Member { get; } = member;

    public string Replacement { get; } = replacement;
}
