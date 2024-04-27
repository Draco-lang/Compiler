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
    private List<(int Value, MutableBox<bool?> Box)>? _boxes;

    public void Add(int toAdd)
    {
        this.MinimumCurrentValue += toAdd;
        if (this._boxes is null) return;
        var i = this._boxes.Count - 1;
        if (i < 0) return;
        while (true)
        {
            var (value, box) = this._boxes![i];
            if (this.MinimumCurrentValue < value) break;
            box.Value = true;
            if (i == 0) break;
            i--;
        }
        this._boxes.RemoveRange(i, this._boxes.Count - i);
    }

    public void Collapse()
    {
        if (this._boxes is not null)
        {
            foreach (var (_, box) in this._boxes ?? Enumerable.Empty<(int Value, MutableBox<bool?> Tcs)>())
            {
                box.Value = false;
            }
            this._boxes = null;
        }

        this.tcs?.SetResult(this.MinimumCurrentValue);
    }

    public SolverTask<int> Collapsed => this.task;

    public Box<bool?> WhenGreaterOrEqual(int number)
    {
        if (this.MinimumCurrentValue >= number) return true;
        this._boxes ??= [];
        var index = this._boxes.BinarySearch((number, null!), Comparer.Instance);
        if (index > 0)
        {
            return this._boxes[index].Box;
        }
        else
        {
            var box = new MutableBox<bool?>(null, true);
            this._boxes.Insert(~index, (number, box));
            return box;
        }
    }

    private class Comparer : IComparer<(int, MutableBox<bool?>)>
    {
        public static Comparer Instance { get; } = new Comparer();
        // reverse comparison.
        public int Compare((int, MutableBox<bool?>) x, (int, MutableBox<bool?>) y) => y.Item1.CompareTo(x.Item1);
    }
}
