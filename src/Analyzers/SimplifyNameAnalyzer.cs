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
        context.RegisterSyntaxNodeAction(
            AnalyzeDottedName,
            SyntaxKind.QualifiedName,
            SyntaxKind.AliasQualifiedName,
            SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeDottedName(SyntaxNodeAnalysisContext context)
    {
        var node = context.Node;

        switch (node.Parent)
        {
            case QualifiedNameSyntax qualifiedName when qualifiedName.Left == node:
            case MemberAccessExpressionSyntax memberAccess when memberAccess.Expression == node:
            case AttributeSyntax:
            case QualifiedCrefSyntax:
                return;
        }

        if (node.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>() is not null
            || node.FirstAncestorOrSelf<UsingDirectiveSyntax>() is not null
            || IsNamespaceDeclarationName(node)
            || node.ContainsDirectives
            || DottedName.Create(node) is not { } dottedName)
        {
            return;
        }

        if (!SyntaxFacts.IsInNamespaceOrTypeContext((ExpressionSyntax)node))
        {
            AnalyzeExpression(context, dottedName);

            return;
        }

        var cancellationToken = context.CancellationToken;
        var semanticModel = context.SemanticModel;
        var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);

        if (symbolInfo.Symbol is not INamedTypeSymbol type
            || type.TypeKind == TypeKind.Error
            || !symbolInfo.CandidateSymbols.IsEmpty)
        {
            return;
        }

        foreach (var form in dottedName.GetForms(type))
        {
            var candidate = semanticModel.GetSpeculativeSymbolInfo(
                node.SpanStart,
                SyntaxFactory.ParseTypeName(dottedName.GetKeptText(form)),
                SpeculativeBindingOption.BindAsTypeOrNamespace);

            if (SymbolEqualityComparer.Default.Equals(candidate.Symbol, type))
            {
                if (!HasErrors(semanticModel, node.Span, cancellationToken))
                {
                    Report(context, form.RemovedSpan, dottedName.GetRemovedText(form));
                }

                return;
            }
        }
    }

    private static bool HasErrors(
        SemanticModel semanticModel,
        TextSpan span,
        CancellationToken cancellationToken)
    {
        foreach (var diagnostic in semanticModel.GetDiagnostics(span, cancellationToken))
        {
            if (diagnostic.DefaultSeverity == DiagnosticSeverity.Error
                && span.Contains(diagnostic.Location.SourceSpan))
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
        TextSpan removedSpan,
        string removedText)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(
                s_rule,
                Location.Create(context.Node.SyntaxTree, removedSpan),
                removedText));
    }
}
