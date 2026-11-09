using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        public bool CrefBindsTo(CrefSyntax topCref, CrefSyntax candidate, ISymbol symbol)
        {
            var rebound = semanticModel.GetSpeculativeSymbolInfo(
                topCref.SpanStart,
                candidate,
                SpeculativeBindingOption.BindAsTypeOrNamespace);

            return SymbolEqualityComparer.Default.Equals(rebound.Symbol, symbol);
        }

        public ITypeSymbol? GetColorColorType(
            ISymbol? receiver,
            int position,
            string text,
            CancellationToken cancellationToken)
        {
            var receiverType = receiver.GetReceiverType(position, cancellationToken);

            if (receiverType is null || !semanticModel.BindsAsType(position, text, receiverType))
            {
                return null;
            }

            return receiverType;
        }

        public ISymbol? GetCrefSymbol(CrefSyntax topCref, CancellationToken cancellationToken)
        {
            var info = semanticModel.GetSymbolInfo(topCref, cancellationToken);

            return info.CandidateSymbols.IsEmpty ? info.Symbol : null;
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

        public bool IsDiagnosticDisabled(string diagnosticId, CancellationToken cancellationToken)
        {
            var options = semanticModel.Compilation.Options;

            if (options.SpecificDiagnosticOptions.TryGetValue(diagnosticId, out var specific)
                && specific == ReportDiagnostic.Suppress)
            {
                return true;
            }

            var provider = options.SyntaxTreeOptionsProvider;

            if (provider is not null)
            {
                if (provider.TryGetDiagnosticValue(
                    semanticModel.SyntaxTree,
                    diagnosticId,
                    cancellationToken,
                    out var severity))
                {
                    return severity == ReportDiagnostic.Suppress;
                }

                if (provider.TryGetGlobalDiagnosticValue(
                    diagnosticId,
                    cancellationToken,
                    out severity))
                {
                    return severity == ReportDiagnostic.Suppress;
                }
            }

            return false;
        }
    }
}
