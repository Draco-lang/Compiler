using System;

namespace Draco.Chr.Rules;

/// <summary>
/// A single head in a rule.
/// </summary>
public sealed class Head
{
    /// <summary>
    /// The variable this head is bound to.
    /// </summary>
    public Var? BoundTo { get; }

    /// <summary>
    /// The object the head asserts.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// The type of the head.
    /// </summary>
    public Type? Type { get; }
}
