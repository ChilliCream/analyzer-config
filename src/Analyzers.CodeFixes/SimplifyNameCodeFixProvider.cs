using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace ChilliCream.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SimplifyNameCodeFixProvider)), Shared]
public sealed class SimplifyNameCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [DiagnosticIds.SimplifyName];

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
                    Resources.SimplifyNameCodeFixTitle,
                    cancellationToken => RemoveAsync(
                        context.Document,
                        diagnostic.Location.SourceSpan,
                        cancellationToken),
                    equivalenceKey: DiagnosticIds.SimplifyName),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> RemoveAsync(
        Document document,
        TextSpan span,
        CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        return document.WithText(text.WithChanges(new TextChange(span, string.Empty)));
    }
}
