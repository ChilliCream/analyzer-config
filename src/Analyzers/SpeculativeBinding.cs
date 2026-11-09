using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ChilliCream.Analyzers;

internal static class SpeculativeBinding
{
    // Each rewrite's Target lies strictly inside its Node, and no two Targets overlap.
    public static bool DropRewritesThatChangeBinding(
        SemanticModel semanticModel,
        SyntaxNode root,
        List<SpeculativeRewrite> rewrites,
        CancellationToken cancellationToken)
    {
        var tracked = new SyntaxNode[rewrites.Count * 2];

        for (var i = 0; i < rewrites.Count; i++)
        {
            tracked[i * 2] = rewrites[i].Node;
            tracked[(i * 2) + 1] = rewrites[i].Target;
        }

        var speculated = root.TrackNodes(tracked);

        foreach (var rewrite in rewrites)
        {
            speculated = speculated.ReplaceNode(
                speculated.GetCurrentNode(rewrite.Target)!,
                rewrite.Replacement);
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
            rewrites.Clear();

            return true;
        }

        var dropped = false;

        for (var i = rewrites.Count - 1; i >= 0; i--)
        {
            var rewrite = rewrites[i];
            var speculatedNode = speculated.GetCurrentNode(rewrite.Node)!;

            if (!SymbolEqualityComparer.IncludeNullability.Equals(
                    speculativeModel.GetSymbolInfo(speculatedNode, cancellationToken).Symbol,
                    rewrite.Symbol)
                || !EnclosingExpressionsKeepBinding(
                    semanticModel,
                    speculativeModel,
                    rewrite.Node,
                    speculatedNode,
                    root,
                    cancellationToken))
            {
                rewrites.RemoveAt(i);
                dropped = true;
            }
        }

        return dropped;
    }

    private static bool EnclosingExpressionsKeepBinding(
        SemanticModel semanticModel,
        SemanticModel speculativeModel,
        ExpressionSyntax node,
        ExpressionSyntax speculatedNode,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        var comparer = SymbolEqualityComparer.IncludeNullability;
        SyntaxNode? original = node;
        SyntaxNode? speculated = speculatedNode;

        while (original is not null && speculated is not null && original != root)
        {
            if (original is ExpressionSyntax originalExpression
                && speculated is ExpressionSyntax speculatedExpression)
            {
                if (original != node)
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

    public static SyntaxNode? GetRoot(ExpressionSyntax node)
    {
        SyntaxNode start = node;
        var ancestor = node.Parent;

        while (ancestor is not null and not MemberDeclarationSyntax)
        {
            if (ancestor is AnonymousFunctionExpressionSyntax)
            {
                start = ancestor;
            }

            ancestor = ancestor.Parent;
        }

        var passedStart = start == node;

        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (passedStart && IsSpeculationRoot(current))
            {
                return current;
            }

            if (!IsComparedNode(current))
            {
                return null;
            }

            passedStart |= current == start;
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
