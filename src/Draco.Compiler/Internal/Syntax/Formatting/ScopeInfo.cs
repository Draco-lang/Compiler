using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal class ScopeInfo : IDisposable
{
    public ScopeInfo? Parent { get; }
    public List<ScopeInfo> Childs { get; } = [];
    private readonly SolverTaskCompletionSource<Unit> _stableTcs = new();

    public ScopeInfo(ScopeInfo? parent, string indentation, SolverTask<FoldPriority>? foldPriority = null)
    {
        this.Parent = parent;
        parent?.Childs.Add(this);
        this.Indentation = indentation;
        this.FoldPriority = foldPriority;
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
    public MaterialisationKind MaterialisationKind { get; set; }
    public CollapsibleInt ItemsCount { get; } = CollapsibleInt.Create();
    public string Indentation { get; }

    public SolverTask<FoldPriority>? FoldPriority { get; }

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
            Debug.Assert(item.FoldPriority!.IsCompleted);
            if (item.FoldPriority.Result == Formatting.FoldPriority.AsSoonAsPossible)
            {
                item.IsMaterialized.Collapse(true);
                return true;
            }
        }

        foreach (var item in this.ThisAndParents)
        {
            if (item.IsMaterialized.Collapsed.IsCompleted) continue;
            Debug.Assert(item.FoldPriority!.IsCompleted);
            if (item.FoldPriority.Result == Formatting.FoldPriority.AsLateAsPossible)
            {
                item.IsMaterialized.Collapse(true);
                return true;
            }
        }
        return false;
    }

    public void Dispose() => this.ItemsCount.Collapse();
}
