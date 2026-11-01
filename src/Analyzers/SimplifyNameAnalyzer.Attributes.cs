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
            || DottedName.Create(name) is not { } dottedName)
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

        if (HasErrors(context.SemanticModel, name.Span, context.CancellationToken))
        {
            return;
        }

        var keptStart = name.SpanStart;

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

        ReportSuffix(context, attribute, dottedName, constructor, keptStart);
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

    private static bool HasRemovableSuffix(SyntaxToken identifier)
    {
        var text = identifier.ValueText;

        return text.Length > AttributeSuffix.Length
            && text.EndsWith(AttributeSuffix, StringComparison.Ordinal)
            && text == identifier.Text
            && SyntaxFacts.GetKeywordKind(
                text.Substring(0, text.Length - AttributeSuffix.Length)) == SyntaxKind.None;
    }

    private static void ReportSuffix(
        SyntaxNodeAnalysisContext context,
        AttributeSyntax attribute,
        DottedName dottedName,
        IMethodSymbol constructor,
        int keptStart)
    {
        var identifier = dottedName.Segments[dottedName.Segments.Length - 1].Identifier;

        if (!HasRemovableSuffix(identifier))
        {
            return;
        }

        var name = attribute.Name;
        var suffixSpan = TextSpan.FromBounds(
            identifier.Span.End - AttributeSuffix.Length,
            identifier.Span.End);
        var nameText = name.ToString();
        var withoutSuffix = nameText.Remove(
            suffixSpan.Start - name.SpanStart,
            suffixSpan.Length);

        if (!BindsConstructor(context.SemanticModel, attribute, withoutSuffix, constructor))
        {
            return;
        }

        if (keptStart != name.SpanStart
            && !BindsConstructor(
                context.SemanticModel,
                attribute,
                withoutSuffix.Substring(keptStart - name.SpanStart),
                constructor))
        {
            return;
        }

        Report(context, new Simplification(suffixSpan, AttributeSuffix));
    }
}
