using Microsoft.CodeAnalysis;

namespace ChilliCream.Analyzers;

internal static class TypeArgumentFilter
{
    public static bool Qualifies(ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                return Qualifies(array.ElementType);

            case ITypeParameterSymbol parameter:
                return parameter.IsValueType;

            case INamedTypeSymbol named when named.TypeKind != TypeKind.Error && named.IsSealed:
                for (var current = named; current is not null; current = current.ContainingType)
                {
                    if (!current.TypeArguments.All(Qualifies))
                    {
                        return false;
                    }
                }

                return true;

            default:
                return false;
        }
    }
}
