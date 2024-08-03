using System;

namespace Draco.Chr.Constraints;

/// <summary>
/// Represents a constraint.
/// </summary>
public interface IConstraint
{
    /// <summary>
    /// The represented constraint value.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// The type of the constraint value.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Checks, if a constraint holds a value of a given type.
    /// </summary>
    /// <param name="type">The type to check for.</param>
    /// <returns>True, if the constraint holds a value of the given type, false otherwise.</returns>
    public bool IsOfType(Type type);
}

/// <summary>
/// Represents a constraint with a known value type.
/// </summary>
/// <typeparam name="T">The type of the constraint value.</typeparam>
public interface IConstraint<T> : IConstraint
{
    /// <summary>
    /// The represented constraint value.
    /// </summary>
    public new T Value { get; }
}
