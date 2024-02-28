using System;
using System.Collections.Immutable;
using Draco.Chr.Constraints;

namespace Draco.Chr.Rules;

/// <summary>
/// High-level utilities for type-safe rule construction.
/// </summary>
public static class RuleFactory
{
    public readonly record struct TypedPropagation<T1>(Propagation Propagation);

    public static TypedPropagation<T1> Propagation<T1>()
        where T1 : notnull =>
        new(new Propagation(ImmutableArray.Create(typeof(T1))));

    public static TypedPropagation<T1> Propagation<T1>(T1 v1)
        where T1 : notnull =>
        new(new Propagation(ImmutableArray.Create(Head.OfValue(v1))));

    public static TypedPropagation<T1> Named<T1>(
        this TypedPropagation<T1> rule,
        string name)
        where T1 : notnull
    {
        rule.Propagation.WithName(name);
        return rule;
    }

    public static TypedPropagation<T1> Guard<T1>(
        this TypedPropagation<T1> rule,
        Func<IConstraint, bool> guard)
        where T1 : notnull
    {
        rule.Propagation.WithGuard(constraints => guard(constraints[0]));
        return rule;
    }

    public static TypedPropagation<T1> Guard<T1>(
        this TypedPropagation<T1> rule,
        Func<IConstraint<T1>, bool> guard)
        where T1 : notnull
    {
        rule.Propagation.WithGuard(constraints => guard((IConstraint<T1>)constraints[0]));
        return rule;
    }

    public static TypedPropagation<T1> Guard<T1>(
        this TypedPropagation<T1> rule,
        Func<T1, bool> guard)
        where T1 : notnull
    {
        rule.Propagation.WithGuard(constraints => guard((T1)constraints[0].Value!));
        return rule;
    }

    public static TypedPropagation<T1> Body<T1>(
        this TypedPropagation<T1> rule,
        Action<IConstraint, ConstraintStore> body)
        where T1 : notnull
    {
        rule.Propagation.WithBody((constraints, store) => body(constraints[0], store));
        return rule;
    }

    public static TypedPropagation<T1> Body<T1>(
        this TypedPropagation<T1> rule,
        Action<IConstraint<T1>, ConstraintStore> body)
        where T1 : notnull
    {
        rule.Propagation.WithBody((constraints, store) => body((IConstraint<T1>)constraints[0], store));
        return rule;
    }

    public static TypedPropagation<T1> Body<T1>(
        this TypedPropagation<T1> rule,
        Action<T1, ConstraintStore> body)
        where T1 : notnull
    {
        rule.Propagation.WithBody((constraints, store) => body((T1)constraints[0].Value!, store));
        return rule;
    }
}
