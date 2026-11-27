using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ChilliCream.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class RemoveThisQualificationAnalyzer : DiagnosticAnalyzer
{
    private const string HelpLinkUri =
        "https://github.com/ChilliCream/analyzer-config/blob/main/docs/rules/CCA0004.md";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticIds.RemoveThisQualification,
        new LocalizableResourceString(
            nameof(Resources.RemoveThisQualificationTitle),
            Resources.ResourceManager,
            typeof(Resources)),
        new LocalizableResourceString(
            nameof(Resources.RemoveThisQualificationMessage),
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
        context.RegisterCodeBlockStartAction<SyntaxKind>(AnalyzeCodeBlockStart);
    }

    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<SyntaxKind> context)
    {
        var candidates = new ConcurrentQueue<Candidate>();

        context.RegisterSyntaxNodeAction(
            nodeContext =>
            {
                if (GetCandidate(
                        nodeContext.SemanticModel,
                        (ThisExpressionSyntax)nodeContext.Node,
                        nodeContext.CancellationToken)
                    is { } candidate)
                {
                    candidates.Enqueue(candidate);
                }
            },
            SyntaxKind.ThisExpression);
        context.RegisterCodeBlockEndAction(
            blockContext => ReportProvenCandidates(blockContext, candidates));
    }

    private static void ReportProvenCandidates(
        CodeBlockAnalysisContext context,
        IEnumerable<Candidate> candidates)
    {
        var semanticModel = context.SemanticModel;
        var cancellationToken = context.CancellationToken;
        List<(SyntaxNode Root, Candidate Candidate)> found = [];

        foreach (var candidate in candidates.OrderBy(candidate => candidate.Node.SpanStart))
        {
            if (SpeculativeBinding.GetRoot((ExpressionSyntax)candidate.Node) is { } root)
            {
                found.Add((root, candidate));
            }
        }

        foreach (var group in found.GroupBy(item => item.Root, item => item.Candidate))
        {
            var proven = new CandidateProof(
                    semanticModel,
                    group.Key,
                    [.. group],
                    CreateRewrite,
                    requireSameParse: true,
                    cancellationToken)
                .Prove();

            foreach (var candidate in group)
            {
                if (proven.Contains(candidate.Node))
                {
                    var memberAccess = (MemberAccessExpressionSyntax)candidate.Node;
                    var span = TextSpan.FromBounds(
                        candidate.Qualifier.SpanStart,
                        memberAccess.OperatorToken.Span.End);

                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            s_rule,
                            Location.Create(memberAccess.SyntaxTree, span),
                            memberAccess.Name.Identifier.ValueText));
                }
            }
        }
    }
}
