using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ChilliCream.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SimplifyNameAnalyzer : DiagnosticAnalyzer
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
    }
}
