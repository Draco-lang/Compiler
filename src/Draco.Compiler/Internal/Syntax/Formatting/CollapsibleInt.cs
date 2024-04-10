using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Solver.Tasks;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal class CollapsibleInt
{
    private readonly SolverTaskCompletionSource<int>? tcs;
    private readonly SolverTask<int> task;
    public int MinimumCurrentValue { get; private set; }
    private CollapsibleInt(SolverTaskCompletionSource<int> tcs)
    {
        this.tcs = tcs;
        this.task = tcs.Task;
    }

    private CollapsibleInt(SolverTask<int> task)
    {
        this.task = task;
    }

    public static CollapsibleInt Create() => new(new SolverTaskCompletionSource<int>());
    public static CollapsibleInt Create(int value) => new(SolverTask.FromResult(value));


    // order by desc
    private List<(int Value, SolverTaskCompletionSource<bool> Tcs)>? _whenTcs;

    public void Add(int toAdd)
    {
        this.MinimumCurrentValue += toAdd;
        if (this._whenTcs is null) return;
        var i = this._whenTcs.Count - 1;
        if (i < 0) return;
        while (true)
        {
            var (value, tcs) = this._whenTcs![i];
            if (this.MinimumCurrentValue < value) break;
            tcs.SetResult(true);
            if (i == 0) break;
            i--;
        }
        this._whenTcs.RemoveRange(i, this._whenTcs.Count - i);
    }

    public void Collapse()
    {
        if (this._whenTcs is not null)
        {
            foreach (var (_, Tcs) in this._whenTcs ?? Enumerable.Empty<(int Value, SolverTaskCompletionSource<bool> Tcs)>())
            {
                Tcs.SetResult(false);
            }
            this._whenTcs = null;
        }

        this.tcs?.SetResult(this.MinimumCurrentValue);
    }

    public SolverTask<int> Collapsed => this.task;

    public SolverTask<bool> WhenGreaterOrEqual(int number)
    {
        if (this.MinimumCurrentValue >= number) return SolverTask.FromResult(true);
        this._whenTcs ??= [];
        var index = this._whenTcs.BinarySearch((number, null!), Comparer.Instance);
        if (index > 0) return this._whenTcs[index].Tcs.Task;
        var tcs = new SolverTaskCompletionSource<bool>();
        this._whenTcs.Insert(~index, (number, tcs));
        return tcs.Task;
    }

    private class Comparer : IComparer<(int, SolverTaskCompletionSource<bool>)>
    {
        public static Comparer Instance { get; } = new Comparer();
        // reverse comparison.
        public int Compare((int, SolverTaskCompletionSource<bool>) x, (int, SolverTaskCompletionSource<bool>) y) => y.Item1.CompareTo(x.Item1);
    }
}
