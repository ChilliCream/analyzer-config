using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ChilliCream.Analyzers;

public sealed partial class SimplifyNameAnalyzer
{
    private static void AnalyzeQualifiedCref(SyntaxNodeAnalysisContext context)
    {
        var qualifiedCref = (QualifiedCrefSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var cancellationToken = context.CancellationToken;
        var symbolInfo = semanticModel.GetSymbolInfo(qualifiedCref, cancellationToken);
        SyntaxNode name;

        if (qualifiedCref.Member is NameMemberCrefSyntax { Parameters: null }
            && symbolInfo.Symbol is INamedTypeSymbol)
        {
            name = qualifiedCref;
        }
        else if (qualifiedCref.Container is QualifiedNameSyntax or AliasQualifiedNameSyntax)
        {
            name = qualifiedCref.Container;
            symbolInfo = semanticModel.GetSymbolInfo(name, cancellationToken);
        }
        else
        {
            return;
        }

        if (DottedName.Create(name) is { } dottedName
            && GetCrefSimplification(
                semanticModel,
                dottedName,
                symbolInfo,
                cancellationToken) is { } simplification)
        {
            Report(context, simplification);
        }
    }

    private static Simplification? GetCrefSimplification(
        SemanticModel semanticModel,
        DottedName dottedName,
        SymbolInfo symbolInfo,
        CancellationToken cancellationToken)
    {
        if (symbolInfo.Symbol is not INamedTypeSymbol type
            || type.TypeKind == TypeKind.Error
            || !symbolInfo.CandidateSymbols.IsEmpty)
        {
            return null;
        }

        var topCref = dottedName.Node
            .AncestorsAndSelf()
            .OfType<CrefSyntax>()
            .Last();
        var original = topCref == dottedName.Node
            ? symbolInfo
            : semanticModel.GetSymbolInfo(topCref, cancellationToken);

        if (original.Symbol is null || !original.CandidateSymbols.IsEmpty)
        {
            return null;
        }

        bool? hasDiagnostics = null;

        foreach (var form in dottedName.GetForms(type))
        {
            var firstKeptParent = form.FirstKept.Parent!;
            var candidateCref = firstKeptParent is NameMemberCrefSyntax
                && firstKeptParent.Parent is QualifiedCrefSyntax qualifiedCref
                ? qualifiedCref.Member.WithoutTrivia()
                : topCref.ReplaceNode(firstKeptParent, form.FirstKept);
            var candidate = semanticModel.GetSpeculativeSymbolInfo(
                topCref.SpanStart,
                candidateCref,
                SpeculativeBindingOption.BindAsTypeOrNamespace);

            if (!SymbolEqualityComparer.Default.Equals(candidate.Symbol, original.Symbol))
            {
                continue;
            }

            hasDiagnostics ??= HasDiagnostics(semanticModel, topCref.Span, cancellationToken);

            if (hasDiagnostics.Value)
            {
                return null;
            }

            if (ContainsReportedTypeArgument(
                semanticModel,
                dottedName.Node,
                form.RemovedSpan,
                cancellationToken))
            {
                continue;
            }

            return new Simplification(form.RemovedSpan, dottedName.GetRemovedText(form));
        }

        return null;
    }

    private static bool HasDiagnostics(
        SemanticModel semanticModel,
        TextSpan span,
        CancellationToken cancellationToken)
    {
        foreach (var diagnostic in semanticModel.GetDiagnostics(span, cancellationToken))
        {
            if (span.Contains(diagnostic.Location.SourceSpan))
            {
                return true;
            }
        }

        return false;
    }
}
