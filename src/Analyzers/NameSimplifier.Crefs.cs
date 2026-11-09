using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ChilliCream.Analyzers;

internal static partial class NameSimplifier
{
    public static Simplification? GetCrefSimplification(
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

        var topCref = dottedName.Node.GetTopCref()!;

        if (semanticModel.GetCrefSymbol(topCref, cancellationToken) is not { } original)
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

            if (!semanticModel.CrefBindsTo(topCref, candidateCref, original))
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
