using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ChilliCream.Analyzers;

public sealed partial class UseDeclaringTypeAnalyzer
{
    private const int MaxCandidatesPerRoot = 8;

    private static List<Candidate>? CollectCandidates(
        SemanticModel semanticModel,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        List<Candidate> candidates = [];

        foreach (var node in root.DescendantNodes())
        {
            if (node is MemberAccessExpressionSyntax memberAccess
                && SpeculativeBinding.GetRoot(memberAccess) == root
                && !IsInsideNameof(semanticModel, memberAccess, cancellationToken)
                && GetCandidate(
                    semanticModel,
                    memberAccess,
                    insideNameof: false,
                    cancellationToken) is { } candidate)
            {
                if (candidates.Count == MaxCandidatesPerRoot)
                {
                    return null;
                }

                candidates.Add(candidate);
            }
        }

        return candidates;
    }

    private static bool KeepsBinding(
        SemanticModel semanticModel,
        Candidate candidate,
        CancellationToken cancellationToken)
    {
        return candidate.Node is ExpressionSyntax node
            && SpeculativeBinding.GetRoot(node) is { } root
            && CollectCandidates(semanticModel, root, cancellationToken) is { Count: > 0 } all
            && new CandidateProof(semanticModel, root, all, cancellationToken)
                .Prove()
                .Contains(node);
    }
}
