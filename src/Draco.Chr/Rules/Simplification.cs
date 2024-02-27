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

    public Simplification(string name, int headCount)
        : base(name, headCount)
    {
    }

    public Simplification(string name, ImmutableArray<Type> headTypes)
        : base(name, headTypes)
    {
    }

    public Simplification(string name, ImmutableArray<Head> headDefinitions)
        : base(name, headDefinitions)
    {
    }

    public override bool Accepts(ImmutableArray<IConstraint> constraints) =>
        constraints.Length == this.HeadCount && this.guard(constraints);

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
