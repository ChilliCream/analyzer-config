using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ChilliCream.Analyzers;

public sealed partial class SimplifyNameAnalyzer
{
    private static bool BindsAsType(
        SemanticModel semanticModel,
        int position,
        string text,
        ITypeSymbol type)
    {
        var candidate = semanticModel.GetSpeculativeSymbolInfo(
            position,
            SyntaxFactory.ParseTypeName(text),
            SpeculativeBindingOption.BindAsTypeOrNamespace);

        return SymbolEqualityComparer.Default.Equals(candidate.Symbol, type);
    }

    private static Simplification? GetExpressionSimplification(
        SemanticModel semanticModel,
        DottedName dottedName,
        CancellationToken cancellationToken)
    {
        var examinedIndex = -1;
        INamedTypeSymbol? type = null;

        for (var i = 0; i < dottedName.Segments.Length; i++)
        {
            var symbol = semanticModel
                .GetSymbolInfo(dottedName.GetPrefix(i), cancellationToken)
                .Symbol;

            if (symbol is INamedTypeSymbol namedType && namedType.TypeKind != TypeKind.Error)
            {
                examinedIndex = i;
                type = namedType;
            }
            else if (symbol is not INamespaceSymbol)
            {
                break;
            }
        }

        if (type is null
            || (examinedIndex == 0 && dottedName.Alias is null)
            || DottedName.Create(dottedName.GetPrefix(examinedIndex)) is not { } examinedName)
        {
            return null;
        }

        var examined = examinedName.Node;
        var simpleName = examinedName.Segments[examinedName.Segments.Length - 1];

        bool? hasErrors = null;

        foreach (var form in examinedName.GetForms(type))
        {
            var keptText = examinedName.GetKeptText(form);
            var candidate = semanticModel
                .GetSpeculativeSymbolInfo(
                    examined.SpanStart,
                    SyntaxFactory.ParseExpression(keptText),
                    SpeculativeBindingOption.BindAsExpression)
                .Symbol;

            if (!SymbolEqualityComparer.Default.Equals(candidate, type)
                && (form.FirstKept != simpleName
                    || !IsColorColorReceiver(
                        semanticModel,
                        examined,
                        keptText,
                        candidate,
                        type,
                        cancellationToken)))
            {
                continue;
            }

            var firstKeptSymbol = form.FirstKept == simpleName
                ? candidate
                : semanticModel
                    .GetSpeculativeSymbolInfo(
                        examined.SpanStart,
                        SyntaxFactory.ParseExpression(form.FirstKept.ToString()),
                        SpeculativeBindingOption.BindAsExpression)
                    .Symbol;

            if (form.FirstKept != simpleName
                && firstKeptSymbol is not INamespaceOrTypeSymbol
                && !IsColorColorQualifier(
                    semanticModel,
                    examined.SpanStart,
                    form.FirstKept,
                    firstKeptSymbol,
                    cancellationToken))
            {
                continue;
            }

            hasErrors ??= HasErrors(semanticModel, examined.Span, cancellationToken);

            if (hasErrors.Value)
            {
                return null;
            }

            if (ContainsReportedTypeArgument(
                semanticModel,
                examined,
                form.RemovedSpan,
                cancellationToken))
            {
                continue;
            }

            return new Simplification(form.RemovedSpan, examinedName.GetRemovedText(form));
        }

        return null;
    }

    private static ITypeSymbol? GetReceiverType(
        ISymbol? receiver,
        int position,
        CancellationToken cancellationToken)
    {
        return receiver switch
        {
            IFieldSymbol field => field.Type,
            ILocalSymbol local
                when !IsDeclaredAfter(local, position, cancellationToken) => local.Type,
            IParameterSymbol parameter => parameter.Type,
            IPropertySymbol property => property.Type,
            _ => null
        };
    }

    private static bool IsColorColorQualifier(
        SemanticModel semanticModel,
        int position,
        SimpleNameSyntax qualifier,
        ISymbol? receiver,
        CancellationToken cancellationToken)
    {
        var receiverType = GetReceiverType(receiver, position, cancellationToken);

        return receiverType is not null
            && BindsAsType(semanticModel, position, qualifier.ToString(), receiverType);
    }

    private static bool IsColorColorReceiver(
        SemanticModel semanticModel,
        SyntaxNode examined,
        string keptText,
        ISymbol? receiver,
        INamedTypeSymbol type,
        CancellationToken cancellationToken)
    {
        var receiverType = GetReceiverType(receiver, examined.SpanStart, cancellationToken);

        if (!SymbolEqualityComparer.Default.Equals(receiverType, type)
            || examined.Parent is not MemberAccessExpressionSyntax memberAccess
            || !memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression)
            || memberAccess.Expression != examined)
        {
            return false;
        }

        var member = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;

        if (member is not (IFieldSymbol or IPropertySymbol or IEventSymbol or INamedTypeSymbol)
            || member.ContainingType?.IsExtension == true)
        {
            return false;
        }

        if (!BindsAsType(semanticModel, examined.SpanStart, keptText, type))
        {
            return false;
        }

        var memberCandidate = semanticModel.GetSpeculativeSymbolInfo(
            examined.SpanStart,
            SyntaxFactory.ParseExpression($"{keptText}.{memberAccess.Name}"),
            SpeculativeBindingOption.BindAsExpression);

        return SymbolEqualityComparer.Default.Equals(memberCandidate.Symbol, member);
    }

    private static bool IsDeclaredAfter(
        ILocalSymbol local,
        int position,
        CancellationToken cancellationToken)
    {
        var references = local.DeclaringSyntaxReferences;

        if (references.IsEmpty)
        {
            return true;
        }

        switch (references[0].GetSyntax(cancellationToken))
        {
            case ForEachStatementSyntax forEach:
                return !forEach.Statement.Span.Contains(position);

            case SingleVariableDesignationSyntax designation
                when designation.FirstAncestorOrSelf<AssignmentExpressionSyntax>() is { } assignment
                    && assignment.Left.Span.Contains(designation.Span):
                return assignment.Span.End > position;

            case var declaration:
                return declaration.Span.End > position;
        }
    }
}
