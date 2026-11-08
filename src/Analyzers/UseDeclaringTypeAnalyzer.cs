using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ChilliCream.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class UseDeclaringTypeAnalyzer : DiagnosticAnalyzer
{
    private const string HelpLinkUri =
        "https://github.com/ChilliCream/analyzer-config/blob/main/docs/rules/CCA0003.md";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticIds.UseDeclaringType,
        new LocalizableResourceString(
            nameof(Resources.UseDeclaringTypeTitle),
            Resources.ResourceManager,
            typeof(Resources)),
        new LocalizableResourceString(
            nameof(Resources.UseDeclaringTypeMessage),
            Resources.ResourceManager,
            typeof(Resources)),
        "Style",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [s_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeQualifiedAccess,
            SyntaxKind.QualifiedName,
            SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeQualifiedAccess(SyntaxNodeAnalysisContext context)
    {
        var node = context.Node;

        if (node is QualifiedNameSyntax qualifiedName
            && qualifiedName.FirstAncestorOrSelf<CrefSyntax>() is not null)
        {
            return;
        }

        var insideNameof = node is MemberAccessExpressionSyntax
            && IsInsideNameof(context.SemanticModel, node, context.CancellationToken);

        if (GetCandidate(
                context.SemanticModel,
                node,
                insideNameof,
                context.CancellationToken) is not { } candidate
            || !IsReportable(
                context.SemanticModel,
                candidate,
                insideNameof,
                context.CancellationToken))
        {
            return;
        }

        var properties = ImmutableDictionary<string, string?>.Empty.Add(
            DiagnosticProperties.Replacement,
            candidate.Replacement);

        context.ReportDiagnostic(
            Diagnostic.Create(
                s_rule,
                candidate.Qualifier.GetLocation(),
                properties,
                candidate.Member.Name,
                candidate.Replacement));
    }

    private static bool IsReportable(
        SemanticModel semanticModel,
        Candidate candidate,
        bool insideNameof,
        CancellationToken cancellationToken)
    {
        if (candidate.Node is QualifiedNameSyntax qualifiedName)
        {
            return !HasOuterTypeNameCandidate(semanticModel, qualifiedName, cancellationToken);
        }

        return insideNameof
            ? NameofKeepsBinding(semanticModel, candidate, cancellationToken)
                && !HasOuterNameofCandidate(semanticModel, candidate, cancellationToken)
            : KeepsBinding(semanticModel, candidate, cancellationToken);
    }
}
