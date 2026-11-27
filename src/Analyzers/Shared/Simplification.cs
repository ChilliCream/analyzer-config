using Microsoft.CodeAnalysis.Text;

namespace ChilliCream.Analyzers;

internal readonly struct Simplification(TextSpan removedSpan, string removedText)
{
    public TextSpan RemovedSpan { get; } = removedSpan;

    public string RemovedText { get; } = removedText;
}
