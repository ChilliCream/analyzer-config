using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace ChilliCream.Analyzers;

internal static class SemanticModelExtensions
{
    extension(SemanticModel semanticModel)
    {
        public bool BindsAsType(int position, string text, ITypeSymbol type)
        {
            var candidate = semanticModel.GetSpeculativeSymbolInfo(
                position,
                SyntaxFactory.ParseTypeName(text),
                SpeculativeBindingOption.BindAsTypeOrNamespace);

            return SymbolEqualityComparer.Default.Equals(candidate.Symbol, type);
        }

        public bool HasErrors(TextSpan span, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in semanticModel.GetDiagnostics(span, cancellationToken))
            {
                if (diagnostic.DefaultSeverity == DiagnosticSeverity.Error
                    && span.Contains(diagnostic.Location.SourceSpan))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
