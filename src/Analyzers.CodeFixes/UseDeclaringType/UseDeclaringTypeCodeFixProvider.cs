using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace ChilliCream.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseDeclaringTypeCodeFixProvider))]
[Shared]
public sealed class UseDeclaringTypeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [DiagnosticIds.UseDeclaringType];

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Properties.GetValueOrDefault(DiagnosticProperties.Replacement)
                is not { } replacement)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    Resources.UseDeclaringTypeCodeFixTitle,
                    cancellationToken => context.Document.ReplaceAsync(
                        diagnostic.Location.SourceSpan,
                        replacement,
                        cancellationToken),
                    equivalenceKey: DiagnosticIds.UseDeclaringType),
                diagnostic);
        }

        return Task.CompletedTask;
    }
}
