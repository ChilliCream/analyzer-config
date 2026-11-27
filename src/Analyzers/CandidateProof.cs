using Microsoft.CodeAnalysis;

namespace ChilliCream.Analyzers;

internal sealed class CandidateProof(
    SemanticModel semanticModel,
    SyntaxNode root,
    List<Candidate> candidates,
    Func<Candidate, SpeculativeRewrite> createRewrite,
    bool requireSameParse,
    CancellationToken cancellationToken)
{
    private readonly SemanticModel _semanticModel = semanticModel;
    private readonly SyntaxNode _root = root;
    private readonly List<Candidate> _candidates = candidates;
    private readonly Func<Candidate, SpeculativeRewrite> _createRewrite = createRewrite;
    private readonly bool _requireSameParse = requireSameParse;
    private readonly CancellationToken _cancellationToken = cancellationToken;
    private readonly int[] _outers = new int[candidates.Count];
    private readonly bool[] _inSet = new bool[candidates.Count];
    private readonly bool[] _heldBack = new bool[candidates.Count];

    public HashSet<SyntaxNode> Prove()
    {
        for (var i = 0; i < _candidates.Count; i++)
        {
            _outers[i] = FindOuter(i);

            if (_outers[i] >= 0 && (_inSet[_outers[i]] || _heldBack[_outers[i]]))
            {
                _heldBack[i] = true;
            }
            else
            {
                _inSet[i] = SurvivesAlone(i);
            }
        }

        while (ProveTogether())
        {
        }

        HashSet<SyntaxNode> proven = [];

        for (var i = 0; i < _candidates.Count; i++)
        {
            if (_inSet[i])
            {
                proven.Add(_candidates[i].Node);
            }
        }

        return proven;
    }

    private int FindOuter(int index)
    {
        for (var j = index - 1; j >= 0; j--)
        {
            if (_candidates[j].Qualifier.Span.Contains(_candidates[index].Node.Span))
            {
                return j;
            }
        }

        return -1;
    }

    private bool ProveTogether()
    {
        List<SpeculativeRewrite> rewrites = [];

        for (var i = 0; i < _candidates.Count; i++)
        {
            if (_inSet[i])
            {
                rewrites.Add(_createRewrite(_candidates[i]));
            }
        }

        if (rewrites.Count < 2
            || !SpeculativeBinding.DropRewritesThatChangeBinding(
                _semanticModel,
                _root,
                rewrites,
                _requireSameParse,
                _cancellationToken))
        {
            return false;
        }

        var survivors = new HashSet<SyntaxNode>();

        foreach (var rewrite in rewrites)
        {
            survivors.Add(rewrite.Node);
        }

        List<int> dropped = [];

        for (var i = 0; i < _candidates.Count; i++)
        {
            if (_inSet[i] && !survivors.Contains(_candidates[i].Node))
            {
                _inSet[i] = false;
                dropped.Add(i);
            }
        }

        foreach (var index in dropped)
        {
            Release(index);
        }

        return true;
    }

    private void Release(int dropped)
    {
        for (var i = 0; i < _candidates.Count; i++)
        {
            if (_outers[i] == dropped && _heldBack[i])
            {
                _heldBack[i] = false;

                if (SurvivesAlone(i))
                {
                    _inSet[i] = true;
                }
                else
                {
                    Release(i);
                }
            }
        }
    }

    private bool SurvivesAlone(int index)
    {
        List<SpeculativeRewrite> rewrites = [_createRewrite(_candidates[index])];

        SpeculativeBinding.DropRewritesThatChangeBinding(
            _semanticModel,
            _root,
            rewrites,
            _requireSameParse,
            _cancellationToken);

        return rewrites.Count == 1;
    }
}
