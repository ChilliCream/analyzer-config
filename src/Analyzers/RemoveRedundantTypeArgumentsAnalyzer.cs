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

    private const int MaxCandidatesPerRoot = 8;

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

        if (GetGenericName(invocation) is not { } name
            || SpeculativeBinding.GetRoot(invocation) is not { } root)
        {
            return;
        }

        var semanticModel = context.SemanticModel;

        if (GetRemovableMethod(semanticModel, invocation, name, context.CancellationToken)
            is not { } method)
        {
            return;
        }

        if (KeepsBinding(semanticModel, root, invocation, method, context.CancellationToken))
        {
            var typeArgumentList = name.TypeArgumentList;

            context.ReportDiagnostic(
                Diagnostic.Create(
                    s_rule,
                    typeArgumentList.GetLocation(),
                    typeArgumentList.ToString()));
        }
    }

    private static SpeculativeRewrite CreateRewrite(
        InvocationExpressionSyntax invocation,
        GenericNameSyntax name,
        IMethodSymbol method)
    {
        return new SpeculativeRewrite(
            invocation,
            name,
            SyntaxFactory.IdentifierName(name.Identifier),
            method);
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

    private static IMethodSymbol? GetRemovableMethod(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        GenericNameSyntax name,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol
            is not IMethodSymbol method)
        {
            return null;
        }

        if (!method.TypeArguments.All(TypeArgumentFilter.Qualifies)
            || !TypeArgumentInference.CanInfer(method)
            || HasNonWhitespaceTrivia(name.TypeArgumentList)
            || semanticModel.HasErrors(invocation.Span, cancellationToken))
        {
            return null;
        }

        return method;
    }

    private static bool HasNonWhitespaceTrivia(TypeArgumentListSyntax typeArgumentList)
    {
        foreach (var trivia in typeArgumentList.DescendantTrivia())
        {
            if (!trivia.IsKind(SyntaxKind.WhitespaceTrivia)
                && !trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return true;
            }
        }

        return false;
    }

    private static bool KeepsBinding(
        SemanticModel semanticModel,
        SyntaxNode root,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        CancellationToken cancellationToken)
    {
        List<SpeculativeRewrite>? others = null;
        var candidates = 1;

        foreach (var node in root.DescendantNodes())
        {
            if (node != invocation
                && node is InvocationExpressionSyntax other
                && GetGenericName(other) is { } otherName
                && SpeculativeBinding.GetRoot(other) == root
                && GetRemovableMethod(semanticModel, other, otherName, cancellationToken)
                    is { } otherMethod)
            {
                if (++candidates > MaxCandidatesPerRoot)
                {
                    return false;
                }

                others ??= [];
                others.Add(CreateRewrite(other, otherName, otherMethod));
            }
        }

        List<SpeculativeRewrite> rewrites =
            [CreateRewrite(invocation, GetGenericName(invocation)!, method)];

        if (SpeculativeBinding.DropRewritesThatChangeBinding(
            semanticModel,
            root,
            rewrites,
            cancellationToken))
        {
            return false;
        }

        if (others is null)
        {
            return true;
        }

        List<SpeculativeRewrite> single = [];

        foreach (var rewrite in others)
        {
            single.Clear();
            single.Add(rewrite);

            if (!SpeculativeBinding.DropRewritesThatChangeBinding(
                semanticModel,
                root,
                single,
                cancellationToken))
            {
                rewrites.Add(rewrite);
            }
        }

        while (rewrites.Count > 1
            && SpeculativeBinding.DropRewritesThatChangeBinding(
                semanticModel,
                root,
                rewrites,
                cancellationToken))
        {
            if (rewrites.Count == 0 || rewrites[0].Node != invocation)
            {
                return false;
            }
        }

        return true;
    }
}
