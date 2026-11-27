using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ChilliCream.Analyzers;

public sealed partial class RemoveThisQualificationAnalyzer
{
    private static SpeculativeRewrite CreateRewrite(Candidate candidate)
    {
        var memberAccess = (MemberAccessExpressionSyntax)candidate.Node;
        var name = memberAccess.Name;
        var replacement = name
            .WithLeadingTrivia(
                candidate.Qualifier
                    .GetLeadingTrivia()
                    .AddRange(memberAccess.OperatorToken.TrailingTrivia)
                    .AddRange(name.GetLeadingTrivia()))
            .WithTrailingTrivia(memberAccess.GetTrailingTrivia());

        return new SpeculativeRewrite(
            memberAccess,
            memberAccess,
            replacement,
            candidate.Member);
    }
}
