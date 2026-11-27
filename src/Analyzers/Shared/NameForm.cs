using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ChilliCream.Analyzers;

internal readonly struct NameForm(SimpleNameSyntax firstKept, TextSpan removedSpan)
{
    public SimpleNameSyntax FirstKept { get; } = firstKept;

    public TextSpan RemovedSpan { get; } = removedSpan;
}
