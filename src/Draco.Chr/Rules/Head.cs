using System;

namespace Draco.Chr.Rules;

/// <summary>
/// A single head in a rule.
/// </summary>
public sealed class Head
{
    /// <summary>
    /// Creates a new head that matches anything.
    /// </summary>
    /// <returns>The new head.</returns>
    public static Head Any() => new();

    /// <summary>
    /// Creates a new head that matches a specific type.
    /// </summary>
    /// <param name="type">The type to match.</param>
    /// <returns>The new head.</returns>
    public static Head OfType(Type type) => new() { Type = type };

    /// <summary>
    /// Creates a new head that matches a specific value.
    /// </summary>
    /// <param name="value">The value to match.</param>
    /// <returns>The new head.</returns>
    public static Head OfValue(object value) => new() { Value = value };

    /// <summary>
    /// The variable this head is bound to.
    /// </summary>
    public Var? BoundTo { get; private set; }

    /// <summary>
    /// The object the head asserts.
    /// </summary>
    public object? Value { get; private init; }

    /// <summary>
    /// The type of the head.
    /// </summary>
    public Type? Type { get; private init; }

    /// <summary>
    /// Computes the head specification.
    /// </summary>
    internal HeadContains HeadContains => (this.Type, this.Value) switch
    {
        (null, null) => HeadContains.Any,
        (not null, null) => HeadContains.Type,
        _ => HeadContains.Value,
    };

    /// <summary>
    /// Binds the head to a variable.
    /// </summary>
    /// <param name="variable">The variable to bind to.</param>
    /// <returns>This instance.</returns>
    public Head Bind(Var variable)
    {
        this.BoundTo = variable;
        return this;
    }
}
