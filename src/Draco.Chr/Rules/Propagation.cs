using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Chr.Constraints;

namespace Draco.Chr.Rules;

/// <summary>
/// A rule that keeps the head constraints and propagates them to the store, adding additional constraints.
/// </summary>
public sealed class Propagation : Rule
{
    public override bool SaveHistory { get; }

    private GuardDelegate guard = _ => true;
    private BodyDelegate body = (_, _) => { };

    public Propagation(int headCount, bool saveHistory)
        : base(headCount)
    {
        this.SaveHistory = saveHistory;
    }

    public Propagation(ImmutableArray<Type> headTypes, bool saveHistory)
        : base(headTypes)
    {
        this.SaveHistory = saveHistory;
    }

    public Propagation(ImmutableArray<Head> headDefinitions, bool saveHistory)
        : base(headDefinitions)
    {
        this.SaveHistory = saveHistory;
    }

    public override bool Accepts(IReadOnlyList<IConstraint> constraints) =>
        this.HeadCount == constraints.Count && this.guard(constraints);

    public override IEnumerable<IConstraint> Apply(ImmutableArray<IConstraint> constraints)
    {
        var store = new ConstraintStore();
        this.body(constraints, store);
        store.AddRange(constraints);
        return store;
    }

    public Propagation WithGuard(GuardDelegate guard)
    {
        this.guard = guard;
        return this;
    }

    public Propagation WithBody(BodyDelegate body)
    {
        this.body = body;
        return this;
    }
}
