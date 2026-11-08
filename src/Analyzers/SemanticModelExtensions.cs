using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ChilliCream.Analyzers;

internal static class SemanticModelExtensions
{
    extension(SemanticModel semanticModel)
    {
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
