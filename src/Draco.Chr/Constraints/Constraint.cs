using System;

namespace Draco.Chr.Constraints;

/// <summary>
/// Utilities for constraint construction.
/// </summary>
public static class Constraint
{
    /// <summary>
    /// Creates a new constraint with the given value.
    /// </summary>
    /// <param name="value">The value of the constraint.</param>
    /// <returns>A new constraint with the given value.</returns>
    public static IConstraint Create<T>(T value)
        where T : notnull => new Constraint<T>(value);
}

/// <summary>
/// A generic constraint implementation.
/// </summary>
/// <typeparam name="T">The type of the constraint.</typeparam>
internal sealed class Constraint<T>(T value) : IConstraint
    where T : notnull
{
    /// <summary>
    /// The value of the constraint.
    /// </summary>
    public T Value { get; } = value;
    object IConstraint.Value => this.Value;
    public Type Type => this.Value.GetType();

    public override string ToString() => this.Value.ToString()!;
    public bool IsOfType(Type type) => type.IsAssignableFrom(this.Type);
}
