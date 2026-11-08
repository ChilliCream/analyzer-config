using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ChilliCream.Analyzers;

internal static class SpanRemover
{
    public static async Task<Document> RemoveAsync(
        Document document,
        TextSpan span,
        CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        return document.WithText(text.WithChanges(new TextChange(span, string.Empty)));
    }
}
