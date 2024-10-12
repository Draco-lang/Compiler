using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// A base class for flow analysis passes.
/// </summary>
/// <typeparam name="TState">The type of the tracked state.</typeparam>
internal abstract class FlowAnalysisPass<TState> : BoundTreeVisitor
{
    // Lattice operations //////////////////////////////////////////////////////

    /// <summary>
    /// The top element of this lattice, which generally represents "reachable, but no information yet",
    /// which is usually the state of the starting point.
    /// </summary>
    public abstract TState Top { get; }

    /// <summary>
    /// The bottom element of this lattice, usually representing "unreachable".
    /// </summary>
    public abstract TState Bottom { get; }

    /// <summary>
    /// Combines two states in a sequential manner.
    /// Generally corresponds to sequential execution of two states.
    /// </summary>
    /// <param name="target">The first state and the state to combine into.</param>
    /// <param name="other">The second state to combine.</param>
    /// <returns>True if the state changed, false otherwise.</returns>
    public abstract bool AndThen(ref TState target, in TState other);

    /// <summary>
    /// Combine two states in a parallel manner.
    /// Corresponds to two branches meeting at the same point.
    /// </summary>
    /// <param name="target">The state to combine into.</param>
    /// <param name="other">The other state to combine.</param>
    /// <returns>True if the state changed, false otherwise.</returns>
    public abstract bool OrElse(ref TState target, in TState other);

    /// <summary>
    /// Deep-copies the given state.
    /// </summary>
    /// <param name="state">The state to deep-copy.</param>
    /// <returns>An equivalent clone of <paramref name="state"/>.</returns>
    public abstract TState Clone(in TState state);

    // Helpers /////////////////////////////////////////////////////////////////

    // TODO

    // Visitors ////////////////////////////////////////////////////////////////

    // TODO
}
