using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ChilliCream.Analyzers;

internal static class SymbolExtensions
{
    extension(ISymbol? receiver)
    {
        public ITypeSymbol? GetReceiverType(int position, CancellationToken cancellationToken)
        {
            return receiver switch
            {
                IFieldSymbol field => field.Type,
                ILocalSymbol local
                    when !IsDeclaredAfter(local, position, cancellationToken) => local.Type,
                IParameterSymbol parameter => parameter.Type,
                IPropertySymbol property => property.Type,
                _ => null
            };
        }
    }

    private static bool IsDeclaredAfter(
        ILocalSymbol local,
        int position,
        CancellationToken cancellationToken)
    {
        var references = local.DeclaringSyntaxReferences;

        if (references.IsEmpty)
        {
            return true;
        }

        switch (references[0].GetSyntax(cancellationToken))
        {
            case ForEachStatementSyntax forEach:
                return !forEach.Statement.Span.Contains(position);

            case SingleVariableDesignationSyntax designation
                when designation.FirstAncestorOrSelf<AssignmentExpressionSyntax>() is { } assignment
                    && assignment.Left.Span.Contains(designation.Span):
                return assignment.Span.End > position;

            case var declaration:
                return declaration.Span.End > position;
        }
    }
}
