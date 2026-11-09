using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ChilliCream.Analyzers;

public sealed partial class UseDeclaringTypeAnalyzer
{
    private static readonly SymbolDisplayFormat[] s_formats =
    [
        new(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers),
        new(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers),
        new(
            typeQualificationStyle:
                SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers),
        new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle:
                SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)
    ];

    private static Candidate? GetCandidate(
        SemanticModel semanticModel,
        SyntaxNode node,
        bool insideNameof,
        CancellationToken cancellationToken)
    {
        if (!TryGetParts(
            node,
            out var qualifier,
            out var right,
            out var option))
        {
            return null;
        }

        if (IsSkippedContext(node) || IsCrefContainer(node) || HasComment(qualifier))
        {
            return null;
        }

        if (semanticModel.GetSymbolInfo(qualifier, cancellationToken).Symbol
            is not INamedTypeSymbol { TypeKind: not TypeKind.Error } qualifierType)
        {
            return null;
        }

        var rightInfo = semanticModel.GetSymbolInfo(right, cancellationToken);
        var member = insideNameof
            ? GetNameofMember(semanticModel, node, rightInfo)
            : rightInfo.CandidateSymbols.IsEmpty ? rightInfo.Symbol : null;

        if (member is not { ContainingType: { } declaringType }
            || SymbolEqualityComparer.Default.Equals(declaringType, qualifierType)
            || declaringType.IsExtension
            || HasTypeArguments(declaringType)
            || IsHiddenFromEditor(declaringType)
            || GetReplacement(
                semanticModel,
                qualifier,
                right,
                declaringType,
                member,
                option,
                cancellationToken) is not { } replacement
            || (!semanticModel.IsDiagnosticDisabled(
                    DiagnosticIds.SimplifyName,
                    cancellationToken)
                && IsWholeQualifierSimplified(
                    semanticModel,
                    node,
                    qualifier,
                    right,
                    cancellationToken))
            || semanticModel.HasErrors(node.Span, cancellationToken))
        {
            return null;
        }

        return new Candidate(node, qualifier, member, replacement);
    }

    private static IEnumerable<string> GetForms(INamedTypeSymbol declaringType)
    {
        var previous = string.Empty;

        if (declaringType.SpecialType == SpecialType.System_Object)
        {
            previous = "object";

            yield return previous;
        }

        foreach (var format in s_formats)
        {
            var form = declaringType.ToDisplayString(format);

            if (form != previous)
            {
                previous = form;

                yield return form;
            }
        }
    }

    private static string? GetReplacement(
        SemanticModel semanticModel,
        ExpressionSyntax qualifier,
        SimpleNameSyntax right,
        INamedTypeSymbol declaringType,
        ISymbol member,
        SpeculativeBindingOption option,
        CancellationToken cancellationToken)
    {
        var position = qualifier.SpanStart;
        var rightText = right.ToString();

        foreach (var form in GetForms(declaringType))
        {
            var qualifierSyntax = Parse(form, option);
            var qualifierInfo = semanticModel.GetSpeculativeSymbolInfo(
                position,
                qualifierSyntax,
                option);

            if (!SymbolEqualityComparer.Default.Equals(qualifierInfo.Symbol, declaringType)
                && (option != SpeculativeBindingOption.BindAsExpression
                    || !IsColorColorForm(
                        semanticModel,
                        position,
                        qualifierSyntax,
                        qualifierInfo.Symbol,
                        declaringType,
                        cancellationToken)))
            {
                continue;
            }

            var nameInfo = semanticModel.GetSpeculativeSymbolInfo(
                position,
                Parse($"{form}.{rightText}", option),
                option);

            if (SymbolEqualityComparer.Default.Equals(nameInfo.Symbol, member)
                || (option == SpeculativeBindingOption.BindAsExpression
                    && HasCandidate(nameInfo, member)))
            {
                return form;
            }
        }

        return null;
    }

    private static bool HasCandidate(SymbolInfo info, ISymbol member)
    {
        var original = member.OriginalDefinition;

        foreach (var candidate in info.CandidateSymbols)
        {
            if (SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, original))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasTypeArguments(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.TypeArguments.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsHiddenFromEditor(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass is
                {
                    Name: "EditorBrowsableAttribute",
                    ContainingNamespace:
                    {
                        Name: "ComponentModel",
                        ContainingNamespace:
                        {
                            Name: "System",
                            ContainingNamespace.IsGlobalNamespace: true
                        }
                    }
                }
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is (int)EditorBrowsableState.Never
                    or (int)EditorBrowsableState.Advanced)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSkippedContext(SyntaxNode node)
    {
        if (node.ContainsDirectives
            || node.FirstAncestorOrSelf<UsingDirectiveSyntax>() is not null)
        {
            return true;
        }

        return node.FirstAncestorOrSelf<AttributeSyntax>()?.Name.Span.Contains(node.Span) == true;
    }

    private static bool IsWholeQualifierSimplified(
        SemanticModel semanticModel,
        SyntaxNode node,
        ExpressionSyntax qualifier,
        SimpleNameSyntax right,
        CancellationToken cancellationToken)
    {
        var name = node;

        while ((name.Parent is QualifiedNameSyntax qualifiedName && qualifiedName.Left == name)
            || (name.Parent is MemberAccessExpressionSyntax memberAccess
                && memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                && memberAccess.Expression == name))
        {
            name = name.Parent;
        }

        return NameSimplifier.GetSimplification(semanticModel, name, cancellationToken)
            is { RemovedSpan: var span }
            && span.Start == qualifier.SpanStart
            && span.End >= right.SpanStart;
    }

    private static ExpressionSyntax Parse(string text, SpeculativeBindingOption option)
    {
        return option == SpeculativeBindingOption.BindAsTypeOrNamespace
            ? SyntaxFactory.ParseTypeName(text)
            : SyntaxFactory.ParseExpression(text);
    }

    private static bool TryGetParts(
        SyntaxNode node,
        out ExpressionSyntax qualifier,
        out SimpleNameSyntax right,
        out SpeculativeBindingOption option)
    {
        switch (node)
        {
            case QualifiedNameSyntax qualifiedName:
                qualifier = qualifiedName.Left;
                right = qualifiedName.Right;
                option = SpeculativeBindingOption.BindAsTypeOrNamespace;

                return true;

            case MemberAccessExpressionSyntax memberAccess:
                qualifier = memberAccess.Expression;
                right = memberAccess.Name;
                option = SpeculativeBindingOption.BindAsExpression;

                return true;

            default:
                qualifier = null!;
                right = null!;
                option = default;

                return false;
        }
    }
}
