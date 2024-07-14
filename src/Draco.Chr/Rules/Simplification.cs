using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Chr.Constraints;

namespace Draco.Chr.Rules;

/// <summary>
/// A rule that removes the head constraints and adds additional constraints to the store.
/// </summary>
public sealed class Simplification : Rule
{
    private GuardDelegate guard = _ => true;
    private BodyDelegate body = (_, _) => { };

    public Simplification(int headCount)
        : base(headCount)
    {
    }

    public Simplification(ImmutableArray<Type> headTypes)
        : base(headTypes)
    {
    }

    public Simplification(ImmutableArray<Head> headDefinitions)
        : base(headDefinitions)
    {
    }

    public override bool Accepts(IReadOnlyList<IConstraint> constraints) =>
        constraints.Count == this.HeadCount && this.guard(constraints);

    public override IEnumerable<IConstraint> Apply(ImmutableArray<IConstraint> constraints)
    {
        var bodyStore = new ConstraintStore();
        this.body(constraints, bodyStore);
        return bodyStore;
    }

    public Simplification WithGuard(GuardDelegate guard)
    {
        this.guard = guard;
        return this;
    }

    public Simplification WithBody(BodyDelegate body)
    {
        this.body = body;
        return this;
    }
}
