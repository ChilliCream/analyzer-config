using Microsoft.CodeAnalysis;

namespace ChilliCream.Analyzers;

internal static class TypeArgumentInference
{
    public static bool CanInfer(IMethodSymbol method)
    {
        var definition = (method.ReducedFrom ?? method).OriginalDefinition;

        foreach (var typeParameter in definition.TypeParameters)
        {
            if (!AnyParameterMentions(definition, typeParameter))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AnyParameterMentions(
        IMethodSymbol method,
        ITypeParameterSymbol typeParameter)
    {
        foreach (var parameter in method.Parameters)
        {
            if (Mentions(parameter.Type, typeParameter))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Mentions(ITypeSymbol type, ITypeParameterSymbol typeParameter)
    {
        switch (type)
        {
            case ITypeParameterSymbol parameter:
                return SymbolEqualityComparer.Default.Equals(parameter, typeParameter);

            case IArrayTypeSymbol array:
                return Mentions(array.ElementType, typeParameter);

            case INamedTypeSymbol named:
                for (var current = named; current is not null; current = current.ContainingType)
                {
                    foreach (var argument in current.TypeArguments)
                    {
                        if (Mentions(argument, typeParameter))
                        {
                            return true;
                        }
                    }
                }

                return false;

            default:
                return false;
        }
    }
}
