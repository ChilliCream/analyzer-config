using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ChilliCream.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class SimplifyNameAnalyzer : DiagnosticAnalyzer
{
    private const string HelpLinkUri =
        "https://github.com/ChilliCream/analyzer-config/blob/main/docs/rules/CCA0001.md";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticIds.SimplifyName,
        new LocalizableResourceString(
            nameof(Resources.SimplifyNameTitle),
            Resources.ResourceManager,
            typeof(Resources)),
        new LocalizableResourceString(
            nameof(Resources.SimplifyNameMessage),
            Resources.ResourceManager,
            typeof(Resources)),
        "Style",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri,
        customTags: WellKnownDiagnosticTags.Unnecessary);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [s_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
        context.RegisterSyntaxNodeAction(
            AnalyzeDottedName,
            SyntaxKind.QualifiedName,
            SyntaxKind.AliasQualifiedName,
            SyntaxKind.SimpleMemberAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeQualifiedCref, SyntaxKind.QualifiedCref);
    }

    private static void AnalyzeDottedName(SyntaxNodeAnalysisContext context)
    {
        if (GetSimplification(
            context.SemanticModel,
            context.Node,
            context.CancellationToken) is { } simplification)
        {
            Report(context, simplification);
        }
    }

    private static bool ContainsReportedTypeArgument(
        SemanticModel semanticModel,
        SyntaxNode node,
        TextSpan removedSpan,
        CancellationToken cancellationToken)
    {
        foreach (var descendant in node.DescendantNodes(removedSpan))
        {
            if (descendant is QualifiedNameSyntax or AliasQualifiedNameSyntax
                && removedSpan.Contains(descendant.Span)
                && IsInTypeArgumentList(descendant, node)
                && GetSimplification(semanticModel, descendant, cancellationToken) is not null)
            {
                return true;
            }
        }

        return false;
    }

    private static Simplification? GetSimplification(
        SemanticModel semanticModel,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        switch (node.Parent)
        {
            case QualifiedNameSyntax qualifiedName when qualifiedName.Left == node:
            case MemberAccessExpressionSyntax memberAccess when memberAccess.Expression == node:
            case AttributeSyntax:
            case QualifiedCrefSyntax:
                return null;
        }

        if (node.FirstAncestorOrSelf<UsingDirectiveSyntax>() is not null
            || IsNamespaceDeclarationName(node)
            || node.ContainsDirectives
            || DottedName.Create(node) is not { } dottedName)
        {
            return null;
        }

        if (node.FirstAncestorOrSelf<CrefSyntax>() is not null)
        {
            return GetCrefSimplification(
                semanticModel,
                dottedName,
                semanticModel.GetSymbolInfo(node, cancellationToken),
                cancellationToken);
        }

        if (!SyntaxFacts.IsInNamespaceOrTypeContext((ExpressionSyntax)node))
        {
            return GetExpressionSimplification(semanticModel, dottedName, cancellationToken);
        }

        var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);

        if (symbolInfo.Symbol is not INamedTypeSymbol type
            || type.TypeKind == TypeKind.Error
            || !symbolInfo.CandidateSymbols.IsEmpty)
        {
            return null;
        }

        bool? hasErrors = null;

        foreach (var form in dottedName.GetForms(type))
        {
            if (IsBrokenCast(node, form))
            {
                continue;
            }

            var candidate = semanticModel.GetSpeculativeSymbolInfo(
                node.SpanStart,
                SyntaxFactory.ParseTypeName(dottedName.GetKeptText(form)),
                SpeculativeBindingOption.BindAsTypeOrNamespace);

            if (!SymbolEqualityComparer.Default.Equals(candidate.Symbol, type))
            {
                continue;
            }

            hasErrors ??= semanticModel.HasErrors(node.Span, cancellationToken);

            if (hasErrors.Value)
            {
                return null;
            }

            if (ContainsReportedTypeArgument(
                semanticModel,
                node,
                form.RemovedSpan,
                cancellationToken))
            {
                continue;
            }

            return new Simplification(form.RemovedSpan, dottedName.GetRemovedText(form));
        }

        return null;
    }

    private static bool IsBrokenCast(SyntaxNode node, NameForm form)
    {
        if (node.Parent is not CastExpressionSyntax cast || cast.Type != node)
        {
            return false;
        }

        var text = cast.ToString();
        var removedStart = form.RemovedSpan.Start - cast.SpanStart;
        var simplified = text.Remove(removedStart, form.RemovedSpan.Length);

        return SyntaxFactory.ParseExpression(simplified) is not CastExpressionSyntax;
    }

    private static bool IsInTypeArgumentList(SyntaxNode name, SyntaxNode root)
    {
        for (var ancestor = name.Parent; ancestor is not null && ancestor != root;
            ancestor = ancestor.Parent)
        {
            if (ancestor is TypeArgumentListSyntax)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNamespaceDeclarationName(SyntaxNode node)
    {
        return node.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>()
            ?.Name.Span.Contains(node.Span) == true;
    }

    private static void Report(
        SyntaxNodeAnalysisContext context,
        Simplification simplification)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(
                s_rule,
                Location.Create(context.Node.SyntaxTree, simplification.RemovedSpan),
                simplification.RemovedText));
    }
}
