using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ChilliCream.Analyzers;

public sealed partial class UseDeclaringTypeAnalyzer
{
    private static ImmutableArray<ISymbol> GetNameofGroup(
        SemanticModel semanticModel,
        SyntaxNode node,
        SymbolInfo rightInfo)
    {
        if (node is MemberAccessExpressionSyntax memberAccess)
        {
            var group = semanticModel.GetMemberGroup(memberAccess);

            if (!group.IsEmpty)
            {
                return group;
            }
        }

        return rightInfo.Symbol is { } symbol && rightInfo.CandidateSymbols.IsEmpty
            ? [symbol]
            : [];
    }

    private static ISymbol? GetNameofMember(
        SemanticModel semanticModel,
        SyntaxNode node,
        SymbolInfo rightInfo)
    {
        var group = GetNameofGroup(semanticModel, node, rightInfo);

        if (group.IsEmpty)
        {
            return null;
        }

        var declaringType = group[0].ContainingType;

        foreach (var member in group)
        {
            if (!SymbolEqualityComparer.Default.Equals(member.ContainingType, declaringType)
                || (!member.IsStatic && member is not INamedTypeSymbol))
            {
                return null;
            }
        }

        return group[0];
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

    private static bool NameofKeepsBinding(
        SemanticModel semanticModel,
        Candidate candidate,
        CancellationToken cancellationToken)
    {
        if (candidate.Node is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        var group = GetNameofGroup(
            semanticModel,
            memberAccess,
            semanticModel.GetSymbolInfo(memberAccess.Name, cancellationToken));
        var info = semanticModel.GetSpeculativeSymbolInfo(
            candidate.Qualifier.SpanStart,
            SyntaxFactory.ParseExpression($"{candidate.Replacement}.{memberAccess.Name}"),
            SpeculativeBindingOption.BindAsExpression);

        if (info.Symbol is { } symbol)
        {
            return group.Length == 1 && SymbolEqualityComparer.Default.Equals(symbol, group[0]);
        }

        return info.CandidateSymbols.Length == group.Length
            && group.All(member => HasCandidate(info, member));
    }
}
