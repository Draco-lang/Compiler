using System.Diagnostics.CodeAnalysis;

namespace Draco.Compiler.Internal.Syntax.Formatting;

public sealed class Scope
{
    private readonly string? indentation;
    private readonly (IReadOnlyList<TokenMetadata> tokens, int indexOfLevelingToken)? levelingToken;
    private readonly FormatterSettings settings;

    [MemberNotNullWhen(true, nameof(levelingToken))]
    [MemberNotNullWhen(false, nameof(indentation))]
    private bool DrivenByLevelingToken => this.levelingToken.HasValue;

    private Scope(Scope? parent, FormatterSettings settings, FoldPriority foldPriority)
    {
        this.Parent = parent;
        this.settings = settings;
        this.FoldPriority = foldPriority;
    }

    public Scope(Scope? parent, FormatterSettings settings, FoldPriority foldPriority, string indentation) : this(parent, settings, foldPriority)
    {
        this.indentation = indentation;
    }

    public Scope(Scope? parent, FormatterSettings settings, FoldPriority foldPriority, (IReadOnlyList<TokenMetadata> tokens, int indexOfLevelingToken) levelingToken)
        : this(parent, settings, foldPriority)
    {
        this.levelingToken = levelingToken;
    }

    public Scope? Parent { get; }

    /// <summary>
    /// Arbitrary data that can be attached to the scope.
    /// Currently only used to group similar binary expressions together.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Represent if the scope is materialized or not.
    /// An unmaterialized scope is a potential scope, which is not folded yet.
    /// <code>items.Select(x => x).ToList()</code> have an unmaterialized scope.
    /// It can be materialized like:
    /// <code>
    /// items
    ///     .Select(x => x)
    ///     .ToList()
    /// </code>
    /// </summary>
    public MutableBox<bool?> IsMaterialized { get; } = new MutableBox<bool?>(null);

    public IEnumerable<string> CurrentTotalIndent
    {
        get
        {
            if (!(this.IsMaterialized.Value ?? false))
            {
                if (this.Parent is null) return [];
                return this.Parent.CurrentTotalIndent;
            }

            if (!this.DrivenByLevelingToken)
            {
                if (this.Parent is null) return [this.indentation];
                return this.Parent.CurrentTotalIndent.Append(this.indentation);
            }

            var (tokens, indexOfLevelingToken) = this.levelingToken.Value;

            int GetStartLineTokenIndex()
            {
                for (var i = indexOfLevelingToken; i >= 0; i--)
                {
                    if (tokens[i].DoesReturnLine?.Value ?? false)
                    {
                        return i;
                    }
                }
                return 0;
            }

            var startLine = GetStartLineTokenIndex();
            var startToken = this.levelingToken.Value.tokens[startLine];
            var stateMachine = new LineStateMachine(string.Concat(startToken.ScopeInfo.CurrentTotalIndent));
            for (var i = startLine; i <= indexOfLevelingToken; i++)
            {
                var curr = this.levelingToken.Value.tokens[i];
                stateMachine.AddToken(curr, this.settings, false);
            }
            var levelingToken = this.levelingToken.Value.tokens[indexOfLevelingToken];
            return [new string(' ', stateMachine.LineWidth - levelingToken.Text.Length)];

        }
    }

    public FoldPriority FoldPriority { get; }

    public IEnumerable<Scope> ThisAndParents => this.Parents.Prepend(this);

    public IEnumerable<Scope> Parents
    {
        get
        {
            if (this.Parent == null) yield break;
            yield return this.Parent;
            foreach (var item in this.Parent.Parents)
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Try to fold a scope by materializing a scope.
    /// </summary>
    /// <returns> The scope that have been fold, else <see langword="null"/> if no scope can be fold. </returns>
    public Scope? Fold()
    {
        foreach (var item in this.ThisAndParents.Reverse())
        {
            if (item.IsMaterialized.Value.HasValue) continue;
            if (item.FoldPriority == FoldPriority.AsSoonAsPossible)
            {
                item.IsMaterialized.Value = true;
                return item;
            }
        }

        foreach (var item in this.ThisAndParents)
        {
            if (item.IsMaterialized.Value.HasValue) continue;
            if (item.FoldPriority == FoldPriority.AsLateAsPossible)
            {
                item.IsMaterialized.Value = true;
                return item;
            }
        }
        return null;
    }

    public override string ToString() => $"{(this.IsMaterialized.Value.HasValue ? this.IsMaterialized.Value.Value ? "M" : "U" : "?")}{this.FoldPriority}{this.indentation?.Length.ToString() ?? "L"}";
}
