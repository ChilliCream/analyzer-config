using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ChilliCream.Analyzers;

internal static class DocumentExtensions
{
    extension(Document document)
    {
        public async Task<Document> ReplaceAsync(
            TextSpan span,
            string replacement,
            CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            return document.WithText(text.WithChanges(new TextChange(span, replacement)));
        }
    }
}
