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
    /// An iteration of a loop continued, as there was still a sequence item.
    /// </summary>
    SequenceItem,

    /// <summary>
    /// An iteration of a loop ended, because the end of the sequence was reached.
    /// </summary>
    SequenceEnd,

    /// <summary>
    /// The comparison is true.
    /// </summary>
    ComparisonTrue,

    /// <summary>
    /// The comparison is false.
    /// </summary>
    ComparisonFalse,
}

/// <summary>
/// A single edge in a control flow graph.
/// </summary>
/// <param name="Kind">The kind of condition on the edge.</param>
/// <param name="Value">The condition value.</param>
/// <param name="Comparison">The comparison value, if any.</param>
internal readonly record struct FlowCondition(
    FlowConditionKind Kind,
    BoundExpression? Value = null,
    BoundComparison? Comparison = null)
{
    /// <summary>
    /// An unconditional edge.
    /// </summary>
    /// <param name="target">The target basic block.</param>
    /// <returns>The new edge.</returns>
    public static readonly FlowCondition Always = new(FlowConditionKind.Always);

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
    /// Constructs a new conditional edge when the sequence has more items.
    /// </summary>
    /// <param name="value">The sequence value.</param>
    /// <returns>The new edge.</returns>
    public static FlowCondition SequenceItem(BoundExpression value) => new(FlowConditionKind.SequenceItem, value);

    /// <summary>
    /// Constructs a new conditional edge when the end of the sequence was reached.
    /// </summary>
    /// <param name="value">The sequence value.</param>
    /// <returns>The new edge.</returns>
    public static FlowCondition SequenceEnd(BoundExpression value) => new(FlowConditionKind.SequenceEnd, value);

    /// <summary>
    /// Constructs a new conditional edge when the comparison is true.
    /// </summary>
    /// <param name="value">The value being compared.</param>
    /// <param name="comparison">The comparison being made.</param>
    /// <returns>The new edge.</returns>
    public static FlowCondition ComparisonTrue(BoundExpression value, BoundComparison comparison) =>
        new(FlowConditionKind.ComparisonTrue, value, comparison);

    /// <summary>
    /// Constructs a new conditional edge when the comparison is false.
    /// </summary>
    /// <param name="value">The value being compared.</param>
    /// <param name="comparison">The comparison being made.</param>
    /// <returns>The new edge.</returns>
    public static FlowCondition ComparisonFalse(BoundExpression value, BoundComparison comparison) =>
        new(FlowConditionKind.ComparisonFalse, value, comparison);
}
