using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

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
                    cancellationToken => context.Document.ReplaceAsync(
                        diagnostic.Location.SourceSpan,
                        string.Empty,
                        cancellationToken),
                    equivalenceKey: DiagnosticIds.SimplifyName),
                diagnostic);
        }

        return Task.CompletedTask;
    }
}
