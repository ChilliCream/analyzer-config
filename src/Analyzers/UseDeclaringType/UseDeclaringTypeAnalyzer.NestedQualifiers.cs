using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ChilliCream.Analyzers;

public sealed partial class UseDeclaringTypeAnalyzer
{
    private static bool HasOuterNameofCandidate(
        SemanticModel semanticModel,
        Candidate candidate,
        CancellationToken cancellationToken)
    {
        var current = candidate.Node;

        while (current.Parent is MemberAccessExpressionSyntax parent
            && parent.Expression == current)
        {
            if (GetCandidate(
                    semanticModel,
                    parent,
                    insideNameof: true,
                    cancellationToken) is { } outer
                && NameofKeepsBinding(semanticModel, outer, cancellationToken))
            {
                return true;
            }

            current = parent;
        }

        return false;
    }

    private static bool HasOuterTypeNameCandidate(
        SemanticModel semanticModel,
        QualifiedNameSyntax node,
        CancellationToken cancellationToken)
    {
        foreach (var ancestor in node.Ancestors())
        {
            switch (ancestor)
            {
                case QualifiedNameSyntax qualifiedName
                    when qualifiedName.Left.Span.Contains(node.Span):
                    if (GetCandidate(
                            semanticModel,
                            qualifiedName,
                            insideNameof: false,
                            cancellationToken) is not null)
                    {
                        return true;
                    }

                    break;

                case MemberAccessExpressionSyntax memberAccess
                    when memberAccess.Expression.Span.Contains(node.Span):
                    if (IsReportedExpression(semanticModel, memberAccess, cancellationToken))
                    {
                        return true;
                    }

                    break;
            }
        }

        return false;
    }

    private static bool IsReportedExpression(
        SemanticModel semanticModel,
        MemberAccessExpressionSyntax memberAccess,
        CancellationToken cancellationToken)
    {
        var insideNameof = IsInsideNameof(semanticModel, memberAccess, cancellationToken);

        return GetCandidate(
                semanticModel,
                memberAccess,
                insideNameof,
                cancellationToken) is { } candidate
            && (insideNameof
                ? NameofKeepsBinding(semanticModel, candidate, cancellationToken)
                : KeepsBinding(semanticModel, candidate, cancellationToken));
    }
}
