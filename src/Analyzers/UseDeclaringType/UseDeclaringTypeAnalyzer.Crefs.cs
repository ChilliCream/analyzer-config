using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ChilliCream.Analyzers;

public sealed partial class UseDeclaringTypeAnalyzer
{
    private static bool CrefKeepsBinding(
        SemanticModel semanticModel,
        Candidate candidate,
        CancellationToken cancellationToken)
    {
        if (candidate.Node.GetTopCref() is not { } topCref
            || semanticModel.GetCrefSymbol(topCref, cancellationToken) is not { } original)
        {
            return false;
        }

        var replacement = SyntaxFactory
            .ParseTypeName(candidate.Replacement)
            .WithTriviaFrom(candidate.Qualifier);

        return semanticModel.CrefBindsTo(
            topCref,
            topCref.ReplaceNode(candidate.Qualifier, replacement),
            original);
    }

    private static bool IsCrefContainer(SyntaxNode node)
    {
        for (var current = node; current.Parent is { } parent; current = parent)
        {
            if (parent is QualifiedCrefSyntax)
            {
                return true;
            }

            if (parent is not QualifiedNameSyntax)
            {
                return false;
            }
        }

        return false;
    }
}
