using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace ChilliCream.Analyzers;

[ExportCodeFixProvider(
    LanguageNames.CSharp,
    Name = nameof(RemoveRedundantTypeArgumentsCodeFixProvider))]
[Shared]
public sealed class RemoveRedundantTypeArgumentsCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [DiagnosticIds.RemoveRedundantTypeArguments];

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    Resources.RemoveRedundantTypeArgumentsCodeFixTitle,
                    cancellationToken => SpanRemover.RemoveAsync(
                        context.Document,
                        diagnostic.Location.SourceSpan,
                        cancellationToken),
                    equivalenceKey: DiagnosticIds.RemoveRedundantTypeArguments),
                diagnostic);
        }

        return Task.CompletedTask;
    }
}
