using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

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
        if (NameSimplifier.GetSimplification(
            context.SemanticModel,
            context.Node,
            context.CancellationToken) is { } simplification)
        {
            Report(context, simplification);
        }
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
