using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// The kinds of conditions on a control flow edge.
/// </summary>
internal enum FlowConditionKind
{
    /// <summary>
    /// Transitions without any condition.
    /// </summary>
    Always,

    /// <summary>
    /// Transitions, when the condition is true.
    /// </summary>
    WhenTrue,

    /// <summary>
    /// Transitions, when the condition is false.
    /// </summary>
    WhenFalse,

    /// <summary>
    /// An iteration of a loop ended, because the end of the sequence was reached.
    /// </summary>
    EndOfSequence,
}

/// <summary>
/// A single edge in a control flow graph.
/// </summary>
/// <param name="Kind">The kind of condition on the edge.</param>
/// <param name="Value">The condition value.</param>
internal readonly record struct FlowCondition(FlowConditionKind Kind, BoundExpression? Value)
{
    /// <summary>
    /// An unconditional edge.
    /// </summary>
    /// <param name="target">The target basic block.</param>
    /// <returns>The new edge.</returns>
    public static readonly FlowCondition Always = new(FlowConditionKind.Always, null);

    /// <summary>
    /// Constructs a new conditional edge when the condition is true.
    /// </summary>
    /// <param name="value">The condition value.</param>
    /// <returns>The new edge.</returns>
    public static FlowCondition WhenTrue(BoundExpression value) => new(FlowConditionKind.WhenTrue, value);

    /// <summary>
    /// Constructs a new conditional edge when the condition is false.
    /// </summary>
    /// <param name="value">The condition value.</param>
    /// <returns>The new edge.</returns>
    public static FlowCondition WhenFalse(BoundExpression value) => new(FlowConditionKind.WhenFalse, value);

    /// <summary>
    /// Constructs a new conditional edge when the end of the sequence was reached.
    /// </summary>
    /// <param name="value">The sequence value.</param>
    /// <returns>The new edge.</returns>
    public static FlowCondition EndOfSequence(BoundExpression value) => new(FlowConditionKind.EndOfSequence, value);
}
