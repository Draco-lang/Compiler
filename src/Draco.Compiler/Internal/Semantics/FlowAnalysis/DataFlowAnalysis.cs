using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Represents a flow direction in DFA.
/// </summary>
internal enum DataFlowDirection
{
    /// <summary>
    /// The analysis goes forward.
    /// </summary>
    Forward,

    /// <summary>
    /// The analysis goes backward.
    /// </summary>
    Backward,
}

/// <summary>
/// Represents a lattice made up of a certain type of elements.
/// </summary>
/// <typeparam name="TElement">The element type of the lattices.</typeparam>
/// <typeparam name="TStatement">The statement type the lattice can handle.</typeparam>
internal interface ILattice<TElement, TStatement>
{
    /// <summary>
    /// The flow direction the lattice is defined for.
    /// </summary>
    public static abstract DataFlowDirection Direction { get; }

    /// <summary>
    /// The identity element of the lattice (also known as the top element).
    /// </summary>
    public static abstract TElement Identity { get; }

    /// <summary>
    /// Deep-clones the given lattice element.
    /// </summary>
    /// <param name="element">The element to clone.</param>
    /// <returns>The clone of <paramref name="element"/>.</returns>
    public static abstract TElement Clone(TElement element);

    /// <summary>
    /// Checks, if two lattice elements are equal.
    /// </summary>
    /// <param name="lhs">The first lattice element to compare.</param>
    /// <param name="rhs">The second lattice element to compare.</param>
    /// <returns>True, if <paramref name="lhs"/> and <paramref name="rhs"/> are equal, false otherwise.</returns>
    public static abstract bool Equals(TElement lhs, TElement rhs);

    /// <summary>
    /// Transfers the given lattice element through the given statement, according to <see cref="Direction"/>.
    /// </summary>
    /// <param name="element">The element to transfer.</param>
    /// <param name="statement">The statement to use for the transition.</param>
    public static abstract void Transfer(ref TElement element, TStatement statement);

    /// <summary>
    /// Joins up the lattice elements from multiple predecessors.
    /// </summary>
    /// <param name="inputs">The lattice elements to join.</param>
    /// <returns>The resulting lattice element.</returns>
    public static abstract TElement Meet(IEnumerable<TElement> inputs);

}

/// <summary>
/// Represents data flow about a basic block.
/// </summary>
/// <typeparam name="TLatticeElement">The lattice element type.</typeparam>
internal sealed class BlockDataFlowInfo<TLatticeElement>
{
    // NOTE: These are fields on purpose, we access references

    /// <summary>
    /// The input info of the block.
    /// </summary>
    public TLatticeElement In;

    /// <summary>
    /// The output info of the block.
    /// </summary>
    public TLatticeElement Out;

    public BlockDataFlowInfo(TLatticeElement @in = default!, TLatticeElement @out = default!)
    {
        this.In = @in;
        this.Out = @out;
    }
}

/// <summary>
/// Implements utilities for data flow analysis.
/// </summary>
internal static class DataFlowAnalysis
{
    /// <summary>
    /// Utility for adding an item to a queue only if the item is not present yet.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="queue">The queue to add to.</param>
    /// <param name="item">The item to add.</param>
    private static void Enqueue<T>(Queue<T> queue, T item)
    {
        if (!queue.Contains(item)) queue.Enqueue(item);
    }

    /// <summary>
    /// Performs data-flow analysis on the given control-flow graph.
    /// </summary>
    /// <typeparam name="TStatement">The statement type.</typeparam>
    /// <typeparam name="TElement">The lattice element type.</typeparam>
    /// <typeparam name="TLattice">The lattice type.</typeparam>
    /// <param name="cfg">The control-flow graph to analyze.</param>
    /// <param name="boundaryCondition">The boundary condition of the enter or exit block, depending on the direction.</param>
    /// <param name="lattice">The lattice to use.</param>
    /// <returns>The information computed for each block.</returns>
    public static ImmutableDictionary<IBasicBlock<TStatement>, BlockDataFlowInfo<TElement>> Analyze<TStatement, TElement, TLattice>(
        IControlFlowGraph<TStatement> cfg,
        TElement boundaryCondition,
        TLattice lattice)
        where TLattice : ILattice<TElement, TStatement>
    {
        var result = ImmutableDictionary.CreateBuilder<IBasicBlock<TStatement>, BlockDataFlowInfo<TElement>>();

        if (TLattice.Direction == DataFlowDirection.Forward)
        {
            // Initialize
            foreach (var b in cfg.Blocks) result[b] = new(@out: TLattice.Identity);
            result[cfg.Entry].Out = boundaryCondition;
            // Add successors to worklist
            var workList = new Queue<IBasicBlock<TStatement>>();
            foreach (var s in cfg.Entry.Successors) workList.Enqueue(s);
            // Work until worklist is empty
            while (workList.TryDequeue(out var b))
            {
                var info = result[b];
                info.In = TLattice.Meet(b.Predecessors.Select(p => result[p].Out));
                var oldOut = info.Out;
                info.Out = Transfer(info.In, b, lattice);
                // If there was a change, enqueue successors
                if (!TLattice.Equals(oldOut, info.Out))
                {
                    foreach (var s in cfg.Entry.Successors) Enqueue(workList, s);
                }
            }
        }
        else
        {
            // Initialize
            foreach (var b in cfg.Blocks) result[b] = new(@in: TLattice.Identity);
            result[cfg.Entry].In = boundaryCondition;
            // Add predecessors to worklist
            var workList = new Queue<IBasicBlock<TStatement>>();
            foreach (var p in cfg.Entry.Predecessors) workList.Enqueue(p);
            // Work until worklist is empty
            while (workList.TryDequeue(out var b))
            {
                var info = result[b];
                info.Out = TLattice.Meet(b.Successors.Select(s => result[s].In));
                var oldIn = info.In;
                info.In = Transfer(info.Out, b, lattice);
                // If there was a change, enqueue predecessors
                if (!TLattice.Equals(oldIn, info.In))
                {
                    foreach (var p in cfg.Entry.Predecessors) Enqueue(workList, p);
                }
            }
        }

        return result.ToImmutable();
    }

    /// <summary>
    /// Transfers the given lattice element through all statements of a basic block.
    /// </summary>
    /// <typeparam name="TStatement">The statement type.</typeparam>
    /// <typeparam name="TElement">The lattice element type.</typeparam>
    /// <typeparam name="TLattice">The lattice type.</typeparam>
    /// <param name="input">The input lattice element.</param>
    /// <param name="block">The basic block to transfer through.</param>
    /// <param name="lattice">The lattice to use.</param>
    /// <returns>The <paramref name="input"/> lattice element ran through <paramref name="block"/>, wit the respect
    /// of flow direction.</returns>
    public static TElement Transfer<TStatement, TElement, TLattice>(
        TElement input,
        IBasicBlock<TStatement> block,
        TLattice lattice)
        where TLattice : ILattice<TElement, TStatement>
    {
        var output = TLattice.Clone(input);
        if (TLattice.Direction == DataFlowDirection.Forward)
        {
            for (var i = 0; i < block.Statements.Count; ++i)
            {
                TLattice.Transfer(ref output, block.Statements[i]);
            }
        }
        else
        {
            for (var i = block.Statements.Count - 1; i >= 0; --i)
            {
                TLattice.Transfer(ref output, block.Statements[i]);
            }
        }
        return output;
    }
}
