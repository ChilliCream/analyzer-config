using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ChilliCream.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RemoveRedundantTypeArgumentsAnalyzer : DiagnosticAnalyzer
{
    private const string HelpLinkUri =
        "https://github.com/ChilliCream/analyzer-config/blob/main/docs/rules/CCA0002.md";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticIds.RemoveRedundantTypeArguments,
        new LocalizableResourceString(
            nameof(Resources.RemoveRedundantTypeArgumentsTitle),
            Resources.ResourceManager,
            typeof(Resources)),
        new LocalizableResourceString(
            nameof(Resources.RemoveRedundantTypeArgumentsMessage),
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
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (GetGenericName(invocation) is not { } name)
        {
            return;
        }

        var semanticModel = context.SemanticModel;

        if (semanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
            is not IMethodSymbol method)
        {
            return;
        }

        if (KeepsBinding(semanticModel, invocation, name, method, context.CancellationToken))
        {
            var typeArgumentList = name.TypeArgumentList;

            context.ReportDiagnostic(
                Diagnostic.Create(
                    s_rule,
                    typeArgumentList.GetLocation(),
                    typeArgumentList.ToString()));
        }
    }

    private static GenericNameSyntax? GetGenericName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            GenericNameSyntax name => name,
            MemberAccessExpressionSyntax { Name: GenericNameSyntax name } => name,
            MemberBindingExpressionSyntax { Name: GenericNameSyntax name } => name,
            _ => null
        };
    }

    private static bool KeepsBinding(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        GenericNameSyntax name,
        IMethodSymbol method,
        CancellationToken cancellationToken)
    {
        var annotation = new SyntaxAnnotation();
        var rewritten = invocation
            .ReplaceNode(name, SyntaxFactory.IdentifierName(name.Identifier))
            .WithAdditionalAnnotations(annotation);

        foreach (var statement in invocation.Ancestors().OfType<StatementSyntax>())
        {
            var speculated = statement.ReplaceNode(invocation, rewritten);

            if (!semanticModel.TryGetSpeculativeSemanticModel(
                statement.SpanStart,
                speculated,
                out var speculativeModel))
            {
                continue;
            }

            var speculatedInvocation = speculated
                .GetAnnotatedNodes(annotation)
                .OfType<InvocationExpressionSyntax>()
                .Single();

            return SymbolEqualityComparer.IncludeNullability.Equals(
                speculativeModel.GetSymbolInfo(speculatedInvocation, cancellationToken).Symbol,
                method);
        }

        return false;
    }
}
