using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// An interface for lattices.
/// </summary>
/// <typeparam name="TSelf">The implementation type.</typeparam>
/// <typeparam name="TStatement">The statement type.</typeparam>
internal interface ILattice<TSelf, TStatement> : IEquatable<TSelf>
    where TSelf : ILattice<TSelf, TStatement>
{
    /// <summary>
    /// The identity element.
    /// </summary>
    public static abstract TSelf Identity { get; }

    /// <summary>
    /// The bottom element.
    /// </summary>
    public static abstract TSelf Bottom { get; }

    /// <summary>
    /// True, if this is the bottom element.
    /// </summary>
    public bool IsBottom { get; }

    /// <summary>
    /// Deep-copies this lattice.
    /// </summary>
    /// <returns>The clone of this lattice.</returns>
    public TSelf Clone();

    /// <summary>
    /// Transfers the lattice to the state after the given statement.
    /// </summary>
    /// <param name="statement">The statement to use for transfered.</param>
    public void Transfer(TStatement statement);

    /// <summary>
    /// Combines this lattice with another one.
    /// </summary>
    /// <param name="input">The input lattice that is merged into this one.</param>
    public void Meet(TSelf input);
}

/// <summary>
/// Utilities for data-flow analysis.
/// </summary>
internal static class DataFlowAnalysis
{
    /// <summary>
    /// Runs data-flow analysis on a given CFG.
    /// </summary>
    /// <typeparam name="TStatement">The statement type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TLattice">The lattice type.</typeparam>
    /// <param name="cfg">The control-flow graph to perform the analysis on.</param>
    /// <param name="initial">The initial lattice.</param>
    /// <returns>The result of the analysis.</returns>
    public static ImmutableDictionary<IBasicBlock<TStatement, TEdge>, TLattice> Analyze<TStatement, TEdge, TLattice>(
        IControlFlowGraph<TStatement, TEdge> cfg,
        TLattice initial)
        where TLattice : ILattice<TLattice, TStatement>
    {
        var dataFlow = ImmutableDictionary.CreateBuilder<IBasicBlock<TStatement, TEdge>, TLattice>();
        var workList = new Queue<IBasicBlock<TStatement, TEdge>>();

        dataFlow.Add(cfg.Entry, initial);
        workList.Enqueue(cfg.Entry);

        while (workList.TryDequeue(out var block))
        {
            var input = dataFlow[block];
            var output = Transfer(block, input);

            if (output.IsBottom) break;

            // Transfer within block
            if (!input.Equals(output))
            {
                dataFlow[block] = output;
                foreach (var successor in block.Successors.Select(s => s.Value))
                {
                    if (!workList.Contains(successor)) workList.Enqueue(successor);
                }
            }

            // Merge successors
            foreach (var successor in block.Successors.Select(s => s.Value))
            {
                if (!dataFlow.TryGetValue(successor, out var successorLattice))
                {
                    successorLattice = TLattice.Identity;
                    dataFlow.Add(successor, successorLattice);
                }
                successorLattice.Meet(output);
            }
        }

        return dataFlow.ToImmutable();
    }

    /// <summary>
    /// Computes the output lattice from the input and the basic block executed.
    /// </summary>
    /// <typeparam name="TStatement">The statement type.</typeparam>
    /// <typeparam name="TEdge">The edge tyoe.</typeparam>
    /// <typeparam name="TLattice">The lattice type.</typeparam>
    /// <param name="block">The basic-block being executed.</param>
    /// <param name="input">The input lattice.</param>
    /// <returns>The resulting lattice, when <paramref name="input"/> is ran through <paramref name="block"/>.</returns>
    public static TLattice Transfer<TStatement, TEdge, TLattice>(
        IBasicBlock<TStatement, TEdge> block,
        TLattice input)
        where TLattice : ILattice<TLattice, TStatement>
    {
        var output = input.Clone();
        foreach (var stmt in block.Statements) output.Transfer(stmt);
        return output;
    }
}
