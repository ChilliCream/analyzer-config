using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ChilliCream.Analyzers;

public sealed partial class SimplifyNameAnalyzer
{
    private const string AttributeSuffix = "Attribute";

    private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
    {
        var attribute = (AttributeSyntax)context.Node;
        var name = attribute.Name;

        if (name.ContainsDirectives
            || DottedName.Create(name) is not { } dottedName
            || (dottedName.Segments.Length == 1
                && dottedName.Alias is null
                && !HasRemovableSuffix(dottedName.Segments[0].Identifier)))
        {
            return;
        }

        var symbolInfo = context.SemanticModel.GetSymbolInfo(
            attribute,
            context.CancellationToken);

        if (symbolInfo.Symbol is not IMethodSymbol
            {
                MethodKind: MethodKind.Constructor
            } constructor
            || !symbolInfo.CandidateSymbols.IsEmpty)
        {
            return;
        }

        var keptStart = name.SpanStart;
        bool? hasErrors = null;

        foreach (var form in dottedName.GetForms(constructor.ContainingType))
        {
            if (!BindsConstructor(
                    context.SemanticModel,
                    attribute,
                    dottedName.GetKeptText(form),
                    constructor))
            {
                continue;
            }

            hasErrors ??= context.SemanticModel.HasErrors(name.Span, context.CancellationToken);

            if (hasErrors.Value)
            {
                return;
            }

            if (ContainsReportedTypeArgument(
                context.SemanticModel,
                name,
                form.RemovedSpan,
                context.CancellationToken))
            {
                continue;
            }

            Report(
                context,
                new Simplification(form.RemovedSpan, dottedName.GetRemovedText(form)));
            keptStart = form.FirstKept.SpanStart;

            break;
        }

        if (GetSuffixSimplification(
            context.SemanticModel,
            attribute,
            dottedName,
            constructor,
            keptStart) is not { } suffix)
        {
            return;
        }

        hasErrors ??= context.SemanticModel.HasErrors(name.Span, context.CancellationToken);

        if (!hasErrors.Value)
        {
            Report(context, suffix);
        }
    }

    private static bool BindsConstructor(
        SemanticModel semanticModel,
        AttributeSyntax attribute,
        string nameText,
        IMethodSymbol constructor)
    {
        var candidate = semanticModel.GetSpeculativeSymbolInfo(
            attribute.SpanStart,
            attribute.WithName(SyntaxFactory.ParseName(nameText)));

        return SymbolEqualityComparer.Default.Equals(candidate.Symbol, constructor);
    }

    private static Simplification? GetSuffixSimplification(
        SemanticModel semanticModel,
        AttributeSyntax attribute,
        DottedName dottedName,
        IMethodSymbol constructor,
        int keptStart)
    {
        var identifier = dottedName.Segments[dottedName.Segments.Length - 1].Identifier;

        if (!HasRemovableSuffix(identifier))
        {
            return null;
        }

        var name = attribute.Name;
        var suffixSpan = TextSpan.FromBounds(
            identifier.Span.End - AttributeSuffix.Length,
            identifier.Span.End);
        var nameText = name.ToString();
        var withoutSuffix = nameText.Remove(
            suffixSpan.Start - name.SpanStart,
            suffixSpan.Length);

        if (!BindsConstructor(semanticModel, attribute, withoutSuffix, constructor))
        {
            return null;
        }

        if (keptStart != name.SpanStart
            && !BindsConstructor(
                semanticModel,
                attribute,
                withoutSuffix.Substring(keptStart - name.SpanStart),
                constructor))
        {
            return null;
        }

        return new Simplification(suffixSpan, AttributeSuffix);
    }

    private static bool HasRemovableSuffix(SyntaxToken identifier)
    {
        var text = identifier.ValueText;

        return text.Length > AttributeSuffix.Length
            && text.EndsWith(AttributeSuffix, StringComparison.Ordinal)
            && text == identifier.Text
            && SyntaxFacts.GetKeywordKind(
                text.Substring(0, text.Length - AttributeSuffix.Length)) == SyntaxKind.None;
    }
}
