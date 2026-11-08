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
        if (context.Node is not QualifiedNameSyntax qualifiedName
            || qualifiedName.FirstAncestorOrSelf<CrefSyntax>() is not null
            || GetCandidate(
                context.SemanticModel,
                qualifiedName,
                context.CancellationToken) is not { } candidate)
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
}
