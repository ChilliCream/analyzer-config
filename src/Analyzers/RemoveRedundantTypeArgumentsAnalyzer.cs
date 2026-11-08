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

        if (!method.TypeArguments.All(TypeArgumentFilter.Qualifies)
            || !TypeArgumentInference.CanInfer(method)
            || HasNonWhitespaceTrivia(name.TypeArgumentList)
            || semanticModel.HasErrors(invocation.Span, context.CancellationToken))
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

    private static bool EnclosingExpressionsKeepBinding(
        SemanticModel semanticModel,
        SemanticModel speculativeModel,
        InvocationExpressionSyntax invocation,
        InvocationExpressionSyntax speculatedInvocation,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        var comparer = SymbolEqualityComparer.IncludeNullability;
        var original = invocation.Parent;
        var speculated = speculatedInvocation.Parent;

        while (original is not null && speculated is not null && original != root)
        {
            if (original is ExpressionSyntax)
            {
                var originalType = semanticModel.GetTypeInfo(original, cancellationToken);
                var speculatedType = speculativeModel.GetTypeInfo(speculated, cancellationToken);

                if (!comparer.Equals(originalType.Type, speculatedType.Type)
                    || !comparer.Equals(originalType.ConvertedType, speculatedType.ConvertedType))
                {
                    return false;
                }

                if (original is not AnonymousFunctionExpressionSyntax
                    && !comparer.Equals(
                        semanticModel.GetSymbolInfo(original, cancellationToken).Symbol,
                        speculativeModel.GetSymbolInfo(speculated, cancellationToken).Symbol))
                {
                    return false;
                }
            }

            original = original.Parent;
            speculated = speculated.Parent;
        }

        return true;
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

    private static SemanticModel? GetSpeculativeModel(
        SemanticModel semanticModel,
        int position,
        SyntaxNode speculated)
    {
        return speculated switch
        {
            StatementSyntax statement =>
                semanticModel.TryGetSpeculativeSemanticModel(
                    position,
                    statement,
                    out var model)
                    ? model
                    : null,
            EqualsValueClauseSyntax equalsValue =>
                semanticModel.TryGetSpeculativeSemanticModel(
                    position,
                    equalsValue,
                    out var model)
                    ? model
                    : null,
            ArrowExpressionClauseSyntax arrow =>
                semanticModel.TryGetSpeculativeSemanticModel(
                    position,
                    arrow,
                    out var model)
                    ? model
                    : null,
            ConstructorInitializerSyntax initializer =>
                semanticModel.TryGetSpeculativeSemanticModel(
                    position,
                    initializer,
                    out var model)
                    ? model
                    : null,
            PrimaryConstructorBaseTypeSyntax baseType =>
                semanticModel.TryGetSpeculativeSemanticModel(
                    position,
                    baseType,
                    out var model)
                    ? model
                    : null,
            _ => null
        };
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

    private static bool IsSpeculationRoot(SyntaxNode node)
    {
        return node switch
        {
            StatementSyntax => true,
            EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax } => true,
            EqualsValueClauseSyntax
            {
                Parent: VariableDeclaratorSyntax
                {
                    Parent: VariableDeclarationSyntax { Parent: BaseFieldDeclarationSyntax }
                }
            } => true,
            ArrowExpressionClauseSyntax
            {
                Parent: BaseMethodDeclarationSyntax
                    or BasePropertyDeclarationSyntax
                    or AccessorDeclarationSyntax
            } => true,
            ConstructorInitializerSyntax => true,
            PrimaryConstructorBaseTypeSyntax => true,
            _ => false
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

        var start = invocation
            .Ancestors()
            .TakeWhile(node => node is not MemberDeclarationSyntax)
            .LastOrDefault(node => node is AnonymousFunctionExpressionSyntax)
            ?? invocation;

        foreach (var root in start.Ancestors().Where(IsSpeculationRoot))
        {
            var speculated = root.ReplaceNode(invocation, rewritten);

            if (GetSpeculativeModel(semanticModel, root.SpanStart, speculated)
                is not { } speculativeModel)
            {
                continue;
            }

            var speculatedInvocation = speculated
                .GetAnnotatedNodes(annotation)
                .OfType<InvocationExpressionSyntax>()
                .Single();

            return SymbolEqualityComparer.IncludeNullability.Equals(
                speculativeModel.GetSymbolInfo(speculatedInvocation, cancellationToken).Symbol,
                method)
                && EnclosingExpressionsKeepBinding(
                    semanticModel,
                    speculativeModel,
                    invocation,
                    speculatedInvocation,
                    root,
                    cancellationToken);
        }

        return false;
    }
}
