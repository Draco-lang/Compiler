using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Chr.Constraints;

namespace Draco.Chr.Rules;

/// <summary>
/// A rule that removes part of the head constraints and adds additional constraints to the store.
/// </summary>
public sealed class Simpagation : Rule
{
    private int HeadRemoveCount => this.HeadCount - this.headKeepCount;

    private SimpagationGuardDelegate guard = (_, _) => true;
    private SimpagationBodyDelegate body = (_, _, _) => { };

    private readonly int headKeepCount;

    public Simpagation(string name, int headKeepCount, int headRemoveCount)
        : base(name, headKeepCount + headRemoveCount)
    {
        this.headKeepCount = headKeepCount;
        this.CheckHeadCounts(nameof(headKeepCount), nameof(headRemoveCount));
    }

    public Simpagation(string name, int headKeepCount, ImmutableArray<Type> headTypes)
        : base(name, headTypes)
    {
        this.headKeepCount = headKeepCount;
        this.CheckHeadCounts(nameof(headKeepCount), nameof(headTypes));
    }

    public Simpagation(string name, int headKeepCount, ImmutableArray<Head> headDefinitions)
        : base(name, headDefinitions)
    {
        this.headKeepCount = headKeepCount;
        this.CheckHeadCounts(nameof(headKeepCount), nameof(headDefinitions));
    }

    private void CheckHeadCounts(string arg1, string arg2)
    {
        if (this.headKeepCount < 1) throw new ArgumentOutOfRangeException(arg1, "at least one head must be kept");
        if (this.HeadRemoveCount < 1) throw new ArgumentOutOfRangeException(arg2, "at least one head must be removed");
    }

    public override bool Accepts(IReadOnlyList<IConstraint> constraints)
    {
        if (constraints.Count != this.HeadCount) return false;

        var headKeep = constraints.Take(this.headKeepCount).ToList();
        var headRemove = constraints.Skip(this.headKeepCount).ToList();

        return this.guard(headKeep, headRemove);
    }

    public override IEnumerable<IConstraint> Apply(ImmutableArray<IConstraint> constraints)
    {
        var headKeep = constraints.Take(this.headKeepCount).ToImmutableArray();
        var headRemove = constraints.Skip(this.headKeepCount).ToImmutableArray();

        var store = new ConstraintStore();
        this.body(headKeep, headRemove, store);
        store.AddRange(headKeep);
        return store;
    }

    public Simpagation WithGuard(SimpagationGuardDelegate guard)
    {
        this.guard = guard;
        return this;
    }

    public Simpagation WithBody(SimpagationBodyDelegate body)
    {
        this.body = body;
        return this;
    }
}
