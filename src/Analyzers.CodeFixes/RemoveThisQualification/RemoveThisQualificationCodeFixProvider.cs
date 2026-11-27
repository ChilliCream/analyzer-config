using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace ChilliCream.Analyzers;

[ExportCodeFixProvider(
    LanguageNames.CSharp,
    Name = nameof(RemoveThisQualificationCodeFixProvider)),
    Shared]
public sealed class RemoveThisQualificationCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [DiagnosticIds.RemoveThisQualification];

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
                    Resources.RemoveThisQualificationCodeFixTitle,
                    cancellationToken => context.Document.ReplaceAsync(
                        diagnostic.Location.SourceSpan,
                        string.Empty,
                        cancellationToken),
                    equivalenceKey: DiagnosticIds.RemoveThisQualification),
                diagnostic);
        }

        return Task.CompletedTask;
    }
}
