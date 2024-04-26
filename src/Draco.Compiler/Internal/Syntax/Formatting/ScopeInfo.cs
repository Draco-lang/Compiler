using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal class ScopeInfo : IDisposable
{
    public ScopeInfo? Parent { get; }
    public List<ScopeInfo> Childs { get; } = [];
    private readonly SolverTaskCompletionSource<Unit> _stableTcs = new();
    private readonly string? indentation;
    private readonly (IReadOnlyList<TokenDecoration> tokens, int indexOfLevelingToken)? levelingToken;

    [MemberNotNullWhen(true, nameof(levelingToken))]
    [MemberNotNullWhen(false, nameof(indentation))]
    private bool DrivenByLevelingToken => this.levelingToken.HasValue;

    public ScopeInfo(ScopeInfo? parent, FoldPriority foldPriority)
    {
        this.Parent = parent;
        this.FoldPriority = foldPriority;
        parent?.Childs.Add(this);
    }

    public ScopeInfo(ScopeInfo? parent, FoldPriority foldPriority, string indentation) : this(parent, foldPriority)
    {
        this.indentation = indentation;
    }

    public ScopeInfo(ScopeInfo? parent, FoldPriority foldPriority, (IReadOnlyList<TokenDecoration> tokens, int indexOfLevelingToken) levelingToken)
        : this(parent, foldPriority)
    {
        this.levelingToken = levelingToken;
    }

    public SolverTask<Unit> WhenStable => this._stableTcs.Task;

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
    public CollapsibleBool IsMaterialized { get; } = CollapsibleBool.Create();
    public CollapsibleInt ItemsCount { get; } = CollapsibleInt.Create();
    public TokenDecoration? TokenDecoration { get; set; }



    public IEnumerable<string> CurrentTotalIndent
    {
        get
        {
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
                    if (tokens[i].DoesReturnLineCollapsible?.Collapsed.Result ?? false)
                    {
                        return i;
                    }
                }
                return 0;
            }

            var startLine = GetStartLineTokenIndex();
            var startToken = this.levelingToken.Value.tokens[startLine];
            var stateMachine = new LineStateMachine(string.Concat(startToken.ScopeInfo.CurrentTotalIndent));
            for (var i = startLine; i < this.levelingToken.Value.tokens.Count; i++)
            {
                var curr = this.levelingToken.Value.tokens[i];
                stateMachine.AddToken(curr, curr.Token);
            }
            return [new string(' ', stateMachine.LineWidth)];

        }
    }

    public FoldPriority FoldPriority { get; }

    public IEnumerable<ScopeInfo> ThisAndAllChilds => this.AllChilds.Prepend(this);
    public IEnumerable<ScopeInfo> AllChilds
    {
        get
        {
            foreach (var child in this.Childs)
            {
                yield return child;
                foreach (var subChild in child.AllChilds)
                {
                    yield return subChild;
                }
            }
        }
    }

    public ScopeInfo Root
    {
        get
        {
            if (this.Parent == null) return this;
            return this.Parent.Root;
        }
    }

    public IEnumerable<ScopeInfo> ThisAndParents => this.Parents.Prepend(this);

    public IEnumerable<ScopeInfo> Parents
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

    public bool Fold()
    {
        foreach (var item in this.ThisAndParents.Reverse())
        {
            if (item.IsMaterialized.Collapsed.IsCompleted) continue;
            if (item.FoldPriority == FoldPriority.AsSoonAsPossible)
            {
                item.IsMaterialized.Collapse(true);
                return true;
            }
        }

        foreach (var item in this.ThisAndParents)
        {
            if (item.IsMaterialized.Collapsed.IsCompleted) continue;
            if (item.FoldPriority == FoldPriority.AsLateAsPossible)
            {
                item.IsMaterialized.Collapse(true);
                return true;
            }
        }
        return false;
    }

    public void Dispose() => this.ItemsCount.Collapse();
}
