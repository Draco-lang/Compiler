namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// A single flow analysis that can be performed on a control flow graph.
/// </summary>
/// <typeparam name="TState">The state type of the domain used in the flow analysis.</typeparam>
internal abstract class FlowAnalysis<TState>(FlowDomain<TState> domain)
{
    protected sealed class BlockState(TState entry, TState exit)
    {
        /// <summary>
        /// The entry state of the block.
        /// </summary>
        public TState Entry = entry;

        /// <summary>
        /// The exit state of the block.
        /// </summary>
        public TState Exit = exit;
    }

    /// <summary>
    /// The domain that is used in the flow analysis.
    /// </summary>
    protected FlowDomain<TState> Domain { get; } = domain;
}
