using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ChilliCream.Analyzers;

public sealed partial class SimplifyNameAnalyzer
{
    private static void AnalyzeQualifiedCref(SyntaxNodeAnalysisContext context)
    {
        var qualifiedCref = (QualifiedCrefSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var cancellationToken = context.CancellationToken;
        var symbolInfo = semanticModel.GetSymbolInfo(qualifiedCref, cancellationToken);
        SyntaxNode name;

        if (qualifiedCref.Member is NameMemberCrefSyntax { Parameters: null }
            && symbolInfo.Symbol is INamedTypeSymbol)
        {
            name = qualifiedCref;
        }
        else if (qualifiedCref.Container is QualifiedNameSyntax or AliasQualifiedNameSyntax)
        {
            name = qualifiedCref.Container;
            symbolInfo = semanticModel.GetSymbolInfo(name, cancellationToken);
        }
        else
        {
            return;
        }

        if (DottedName.Create(name) is { } dottedName
            && NameSimplifier.GetCrefSimplification(
                semanticModel,
                dottedName,
                symbolInfo,
                cancellationToken) is { } simplification)
        {
            Report(context, simplification);
        }
    }
}
