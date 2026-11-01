using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ChilliCream.Analyzers;

internal sealed class DottedName
{
    private DottedName(
        AliasQualifiedNameSyntax? alias,
        SyntaxNode node,
        ImmutableArray<SimpleNameSyntax> segments)
    {
        Alias = alias;
        Node = node;
        Segments = segments;
    }

    public AliasQualifiedNameSyntax? Alias { get; }

    public SyntaxNode Node { get; }

    public ImmutableArray<SimpleNameSyntax> Segments { get; }

    public static DottedName? Create(SyntaxNode node)
    {
        var segments = ImmutableArray.CreateBuilder<SimpleNameSyntax>();
        AliasQualifiedNameSyntax? alias = null;

        if (!TryAddSegments(node, segments, ref alias))
        {
            return null;
        }

        return new DottedName(alias, node, segments.ToImmutable());
    }

    public ImmutableArray<NameForm> GetForms(INamedTypeSymbol type)
    {
        if (Alias?.Alias.Identifier.IsKind(SyntaxKind.GlobalKeyword) == false)
        {
            return [];
        }

        for (var i = 0; i < Segments.Length - 1; i++)
        {
            if (Segments[i] is GenericNameSyntax { IsUnboundGenericName: true })
            {
                return [];
            }
        }

        var forms = ImmutableArray.CreateBuilder<NameForm>();

        if (Segments.Length >= 2 || Alias is not null)
        {
            AddForm(forms, Segments[Segments.Length - 1]);
        }

        var containingTypeCount = 0;

        for (var containingType = type.ContainingType;
            containingType is not null;
            containingType = containingType.ContainingType)
        {
            containingTypeCount++;
        }

        var index = Segments.Length - (containingTypeCount + 1);

        if (containingTypeCount >= 1 && index >= 1)
        {
            AddForm(forms, Segments[index]);
        }

        if (Alias is not null && Segments.Length >= 2)
        {
            AddForm(forms, Segments[0]);
        }

        return forms.ToImmutable();
    }

    public string GetKeptText(NameForm form)
    {
        return Node.ToString().Substring(form.FirstKept.SpanStart - Node.SpanStart);
    }

    public SyntaxNode GetPrefix(int segmentIndex)
    {
        if (segmentIndex == 0)
        {
            return (SyntaxNode?)Alias ?? Segments[0];
        }

        return Segments[segmentIndex].Parent!;
    }

    public string GetRemovedText(NameForm form)
    {
        var text = new StringBuilder();

        foreach (var token in Node.DescendantTokens(form.RemovedSpan))
        {
            if (form.RemovedSpan.Contains(token.Span))
            {
                text.Append(token.Text);
            }
        }

        return text.ToString();
    }

    private void AddForm(ImmutableArray<NameForm>.Builder forms, SimpleNameSyntax firstKept)
    {
        var removedSpan = TextSpan.FromBounds(Node.SpanStart, firstKept.SpanStart);

        foreach (var trivia in Node.DescendantTrivia(removedSpan))
        {
            if (removedSpan.Contains(trivia.Span)
                && (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                    || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)))
            {
                return;
            }
        }

        forms.Add(new NameForm(firstKept, removedSpan));
    }

    private static bool TryAddSegments(
        SyntaxNode node,
        ImmutableArray<SimpleNameSyntax>.Builder segments,
        ref AliasQualifiedNameSyntax? alias)
    {
        switch (node)
        {
            case QualifiedNameSyntax qualifiedName:
                if (!TryAddSegments(qualifiedName.Left, segments, ref alias))
                {
                    return false;
                }

                segments.Add(qualifiedName.Right);

                return true;

            case MemberAccessExpressionSyntax memberAccess
                when memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression):
                if (!TryAddSegments(memberAccess.Expression, segments, ref alias))
                {
                    return false;
                }

                segments.Add(memberAccess.Name);

                return true;

            case QualifiedCrefSyntax
            {
                Member: NameMemberCrefSyntax { Parameters: null, Name: SimpleNameSyntax name }
            } qualifiedCref:
                if (!TryAddSegments(qualifiedCref.Container, segments, ref alias))
                {
                    return false;
                }

                segments.Add(name);

                return true;

            case AliasQualifiedNameSyntax aliasQualifiedName when segments.Count == 0:
                alias = aliasQualifiedName;
                segments.Add(aliasQualifiedName.Name);

                return true;

            case IdentifierNameSyntax or GenericNameSyntax:
                segments.Add((SimpleNameSyntax)node);

                return true;

            default:
                return false;
        }
    }
}
