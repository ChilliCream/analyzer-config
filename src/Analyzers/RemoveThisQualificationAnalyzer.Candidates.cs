using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ChilliCream.Analyzers;

public sealed partial class RemoveThisQualificationAnalyzer
{
    private static Candidate? GetCandidate(
        SemanticModel semanticModel,
        ThisExpressionSyntax thisExpression,
        CancellationToken cancellationToken)
    {
        if (thisExpression.Parent is not MemberAccessExpressionSyntax memberAccess
            || !memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression)
            || memberAccess.Expression != thisExpression)
        {
            return null;
        }

        var qualificationSpan = TextSpan.FromBounds(
            memberAccess.SpanStart,
            memberAccess.Name.SpanStart);

        if (memberAccess.HasComment(qualificationSpan)
            || memberAccess.HasDirective(qualificationSpan)
            || IsFieldKeyword(memberAccess))
        {
            return null;
        }

        var info = semanticModel.GetSymbolInfo(memberAccess, cancellationToken);

        if (info.Symbol is not { IsStatic: false } member
            || !info.CandidateSymbols.IsEmpty
            || member.Kind is not (SymbolKind.Field
                or SymbolKind.Property
                or SymbolKind.Method
                or SymbolKind.Event)
            || member is IMethodSymbol { MethodKind: MethodKind.ReducedExtension }
            || member.ContainingType is { IsExtension: true })
        {
            return null;
        }

        var visible = semanticModel
            .LookupSymbols(thisExpression.SpanStart, name: memberAccess.Name.Identifier.ValueText)
            .Any(symbol => SymbolEqualityComparer.Default.Equals(
                symbol.OriginalDefinition,
                member.OriginalDefinition));

        if (!visible
            || semanticModel.HasErrors(memberAccess.Span, cancellationToken)
            || ShadowsPrimaryConstructorParameter(
                semanticModel,
                thisExpression,
                member,
                cancellationToken))
        {
            return null;
        }

        return new Candidate(memberAccess, thisExpression, member, string.Empty);
    }

    private static bool IsFieldKeyword(MemberAccessExpressionSyntax memberAccess)
    {
        if (memberAccess.Name.Identifier.Text != "field")
        {
            return false;
        }

        foreach (var ancestor in memberAccess.Ancestors())
        {
            if (ancestor is AccessorDeclarationSyntax
                {
                    Parent.Parent: PropertyDeclarationSyntax or IndexerDeclarationSyntax
                }
                or ArrowExpressionClauseSyntax
                {
                    Parent: PropertyDeclarationSyntax or IndexerDeclarationSyntax
                })
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShadowsPrimaryConstructorParameter(
        SemanticModel semanticModel,
        ThisExpressionSyntax thisExpression,
        ISymbol member,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetTypeInfo(thisExpression, cancellationToken).Type
                is not INamedTypeSymbol type
            || SymbolEqualityComparer.Default.Equals(
                member.ContainingType.OriginalDefinition,
                type.OriginalDefinition))
        {
            return false;
        }

        foreach (var constructor in type.InstanceConstructors)
        {
            if (!constructor.IsImplicitlyDeclared
                && constructor.DeclaringSyntaxReferences.Any(
                    reference => reference.GetSyntax(cancellationToken) is TypeDeclarationSyntax))
            {
                return constructor.Parameters.Any(
                    parameter => string.Equals(
                        parameter.Name,
                        member.Name,
                        StringComparison.Ordinal));
            }
        }

        return false;
    }
}
