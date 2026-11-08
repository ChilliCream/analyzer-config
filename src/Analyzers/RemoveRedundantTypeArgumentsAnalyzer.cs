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
            || GetSpeculationRoot(invocation) is not { } root)
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

    private static bool DropRemovalsThatChangeBinding(
        SemanticModel semanticModel,
        SyntaxNode root,
        List<(InvocationExpressionSyntax Invocation, IMethodSymbol Method)> removals,
        CancellationToken cancellationToken)
    {
        var invocations = new SyntaxNode[removals.Count];

        for (var i = 0; i < removals.Count; i++)
        {
            invocations[i] = removals[i].Invocation;
        }

        var speculated = root.TrackNodes(invocations);

        foreach (var (invocation, _) in removals)
        {
            var name = GetGenericName(speculated.GetCurrentNode(invocation)!)!;
            speculated = speculated.ReplaceNode(
                name,
                SyntaxFactory.IdentifierName(name.Identifier));
        }

        if (GetSpeculativeModel(semanticModel, root.SpanStart, speculated)
                is not { } speculativeModel
            || !RootKeepsBinding(
                semanticModel,
                speculativeModel,
                root,
                speculated,
                cancellationToken))
        {
            removals.Clear();

            return true;
        }

        var dropped = false;

        for (var i = removals.Count - 1; i >= 0; i--)
        {
            var (invocation, method) = removals[i];
            var speculatedInvocation = speculated.GetCurrentNode(invocation)!;

            if (!SymbolEqualityComparer.IncludeNullability.Equals(
                    speculativeModel.GetSymbolInfo(speculatedInvocation, cancellationToken).Symbol,
                    method)
                || !EnclosingExpressionsKeepBinding(
                    semanticModel,
                    speculativeModel,
                    invocation,
                    speculatedInvocation,
                    root,
                    cancellationToken))
            {
                removals.RemoveAt(i);
                dropped = true;
            }
        }

        return dropped;
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
        SyntaxNode? original = invocation;
        SyntaxNode? speculated = speculatedInvocation;

        while (original is not null && speculated is not null && original != root)
        {
            if (original is ExpressionSyntax originalExpression
                && speculated is ExpressionSyntax speculatedExpression)
            {
                if (original != invocation)
                {
                    var originalType = semanticModel.GetTypeInfo(original, cancellationToken);
                    var speculatedType = speculativeModel.GetTypeInfo(
                        speculated,
                        cancellationToken);

                    if (!comparer.Equals(originalType.Type, speculatedType.Type)
                        || !comparer.Equals(
                            originalType.ConvertedType,
                            speculatedType.ConvertedType))
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

                if (original.Parent.IsKind(SyntaxKind.CollectionInitializerExpression)
                    && !comparer.Equals(
                        semanticModel
                            .GetCollectionInitializerSymbolInfo(
                                originalExpression,
                                cancellationToken)
                            .Symbol,
                        speculativeModel
                            .GetCollectionInitializerSymbolInfo(
                                speculatedExpression,
                                cancellationToken)
                            .Symbol))
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

    private static SyntaxNode? GetSpeculationRoot(InvocationExpressionSyntax invocation)
    {
        SyntaxNode start = invocation;
        var ancestor = invocation.Parent;

        while (ancestor is not null and not MemberDeclarationSyntax)
        {
            if (ancestor is AnonymousFunctionExpressionSyntax)
            {
                start = ancestor;
            }

            ancestor = ancestor.Parent;
        }

        var passedStart = start == invocation;

        for (var node = invocation.Parent; node is not null; node = node.Parent)
        {
            if (passedStart && IsSpeculationRoot(node))
            {
                return node;
            }

            if (!IsComparedNode(node))
            {
                return null;
            }

            passedStart |= node == start;
        }

        return null;
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

    private static bool IsComparedNode(SyntaxNode node)
    {
        return node switch
        {
            ExpressionSyntax => true,
            ArgumentSyntax => true,
            ArgumentListSyntax => true,
            BracketedArgumentListSyntax => true,
            StatementSyntax => true,
            EqualsValueClauseSyntax => true,
            VariableDeclaratorSyntax => true,
            VariableDeclarationSyntax => true,
            ArrowExpressionClauseSyntax => true,
            SwitchExpressionArmSyntax => true,
            InterpolationSyntax => true,
            ElseClauseSyntax => true,
            SwitchSectionSyntax => true,
            CatchClauseSyntax => true,
            FinallyClauseSyntax => true,
            FromClauseSyntax { Parent: QueryExpressionSyntax query } fromClause =>
                query.FromClause == fromClause,
            _ => false
        };
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
        SyntaxNode root,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        CancellationToken cancellationToken)
    {
        List<(InvocationExpressionSyntax Invocation, IMethodSymbol Method)>? others = null;
        var candidates = 1;

        foreach (var node in root.DescendantNodes())
        {
            if (node != invocation
                && node is InvocationExpressionSyntax other
                && GetGenericName(other) is { } otherName
                && GetSpeculationRoot(other) == root
                && GetRemovableMethod(semanticModel, other, otherName, cancellationToken)
                    is { } otherMethod)
            {
                if (++candidates > MaxCandidatesPerRoot)
                {
                    return false;
                }

                others ??= [];
                others.Add((other, otherMethod));
            }
        }

        List<(InvocationExpressionSyntax Invocation, IMethodSymbol Method)> removals =
            [(invocation, method)];

        if (DropRemovalsThatChangeBinding(semanticModel, root, removals, cancellationToken))
        {
            return false;
        }

        if (others is null)
        {
            return true;
        }

        List<(InvocationExpressionSyntax Invocation, IMethodSymbol Method)> single = [];

        foreach (var removal in others)
        {
            single.Clear();
            single.Add(removal);

            if (!DropRemovalsThatChangeBinding(semanticModel, root, single, cancellationToken))
            {
                removals.Add(removal);
            }
        }

        while (removals.Count > 1
            && DropRemovalsThatChangeBinding(semanticModel, root, removals, cancellationToken))
        {
            if (removals.Count == 0 || removals[0].Invocation != invocation)
            {
                return false;
            }
        }

        return true;
    }

    private static bool RootKeepsBinding(
        SemanticModel semanticModel,
        SemanticModel speculativeModel,
        SyntaxNode root,
        SyntaxNode speculatedRoot,
        CancellationToken cancellationToken)
    {
        var comparer = SymbolEqualityComparer.IncludeNullability;

        return root switch
        {
            StatementSyntax => true,
            EqualsValueClauseSyntax => true,
            ArrowExpressionClauseSyntax => true,
            ConstructorInitializerSyntax initializer =>
                comparer.Equals(
                    semanticModel.GetSymbolInfo(initializer, cancellationToken).Symbol,
                    speculativeModel
                        .GetSymbolInfo(
                            (ConstructorInitializerSyntax)speculatedRoot,
                            cancellationToken)
                        .Symbol),
            PrimaryConstructorBaseTypeSyntax baseType =>
                comparer.Equals(
                    semanticModel.GetSymbolInfo(baseType, cancellationToken).Symbol,
                    speculativeModel
                        .GetSymbolInfo(
                            (PrimaryConstructorBaseTypeSyntax)speculatedRoot,
                            cancellationToken)
                        .Symbol),
            _ => false
        };
    }
}
