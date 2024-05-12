using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Draco.Compiler.Internal.Syntax.Formatting;

/// <summary>
/// Represent an indentation level in the code.
/// </summary>
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

    /// <summary>
    /// Create a scope that add a new indentation level.
    /// </summary>
    /// <param name="parent">The parent scope.</param>
    /// <param name="settings">The settings of the formatter.</param>
    /// <param name="foldPriority">The fold priority of the scope.</param>
    /// <param name="indentation">The indentation this scope will add.</param>
    public Scope(Scope? parent, FormatterSettings settings, FoldPriority foldPriority, string indentation) : this(parent, settings, foldPriority)
    {
        this.indentation = indentation;
    }

    /// <summary>
    /// Create a scope that override the indentation level and follow the position of a token instead.
    /// </summary>
    /// <param name="parent">The parent scope.</param>
    /// <param name="settings">The settings of the formatter.</param>
    /// <param name="foldPriority">The fold priority of the scope.</param>
    /// <param name="levelingToken">The list of the tokens of the formatter and the index of the token to follow the position.</param>
    public Scope(Scope? parent, FormatterSettings settings, FoldPriority foldPriority, (IReadOnlyList<TokenMetadata> tokens, int indexOfLevelingToken) levelingToken)
        : this(parent, settings, foldPriority)
    {
        this.levelingToken = levelingToken;
    }

    /// <summary>
    /// The parent scope of this scope.
    /// </summary>
    public Scope? Parent { get; }

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

    /// <summary>
    /// All the indentation parts of the current scope and it's parents.
    /// </summary>
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

    /// <summary>
    /// The fold priority of the scope. It's used to determine which scope should be folded first when folding.
    /// </summary>
    public FoldPriority FoldPriority { get; }

    /// <summary>
    /// All the parents of this scope, plus this scope.
    /// </summary>
    public IEnumerable<Scope> ThisAndParents => this.Parents.Prepend(this);

    /// <summary>
    /// All the parents of this scope.
    /// </summary>
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
        var asSoonAsPossible = this.ThisAndParents
            .Reverse()
            .Where(item => !item.IsMaterialized.Value.HasValue)
            .Where(item => item.FoldPriority == FoldPriority.AsSoonAsPossible)
            .FirstOrDefault();

        if (asSoonAsPossible != null)
        {
            asSoonAsPossible.IsMaterialized.Value = true;
            return asSoonAsPossible;
        }

        var asLateAsPossible = this.ThisAndParents
            .Where(item => !item.IsMaterialized.Value.HasValue)
            .Where(item => item.FoldPriority == FoldPriority.AsLateAsPossible)
            .FirstOrDefault();

        if (asLateAsPossible != null)
        {
            asLateAsPossible.IsMaterialized.Value = true;
            return asLateAsPossible;
        }

        return null;
    }

    /// <summary>
    /// A debug string.
    /// </summary>
    public override string ToString()
    {
        var materialized = (this.IsMaterialized.Value.HasValue ? this.IsMaterialized.Value.Value ? "M" : "U" : "?");
        return $"{materialized}{this.FoldPriority}{this.indentation?.Length.ToString() ?? "L"}";
    }
}
