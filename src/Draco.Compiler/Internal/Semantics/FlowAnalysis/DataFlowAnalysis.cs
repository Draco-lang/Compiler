using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Stores information about the in and out state during data flow analysis.
/// </summary>
/// <typeparam name="TElement">The lattice element type.</typeparam>
internal sealed class DataFlowInfo<TElement>
{
    /// <summary>
    /// The input, before the corresponding element.
    /// </summary>
    public TElement In { get; set; }

    /// <summary>
    /// The output, after the corresponding element.
    /// </summary>
    public TElement Out { get; set; }

    public DataFlowInfo(TElement @in = default!, TElement @out = default!)
    {
        this.In = @in;
        this.Out = @out;
    }
}

/// <summary>
/// Performs data-flow analysis using a lattice.
/// </summary>
internal static class DataFlowAnalysis
{
    private static void Enqueue<T>(Queue<T> queue, T item)
    {
        if (!queue.Contains(item)) queue.Enqueue(item);
    }

    private static TElement Meet<TElement>(
        ILattice<TElement> lattice,
        ISet<DataFlowOperation> ops,
        Func<DataFlowOperation, TElement> selector)
    {
        if (ops.Count == 0) return lattice.Identity;
        if (ops.Count == 1) return selector(ops.Single());
        return ops.Select(selector).Aggregate(lattice.Meet);
    }

    public static ImmutableDictionary<Ast, DataFlowInfo<TElement>> Analyze<TElement>(
        ILattice<TElement> lattice,
        DataFlowGraph graph)
    {
        var result = ImmutableDictionary.CreateBuilder<Ast, DataFlowInfo<TElement>>(ReferenceEqualityComparer.Instance);

        if (lattice.Direction == FlowDirection.Forward)
        {
            // Initialize
            var workList = new Queue<DataFlowOperation>();
            foreach (var op in graph.Operations)
            {
                result[op.Node] = new(
                    @in: lattice.Identity,
                    @out: lattice.Transfer(op.Node));
                workList.Enqueue(op);
            }
            // Work until worklist is empty
            while (workList.TryDequeue(out var op))
            {
                var info = result[op.Node];
                // Set the input to the meeting of predecessors
                info.In = Meet(lattice, op.Predecessors, i => result[i.Node].Out);
                var oldOut = info.Out;
                info.Out = lattice.Join(info.In, oldOut);
                // If there was a change, enqueue successors
                if (!lattice.Equals(oldOut, info.Out))
                {
                    foreach (var succ in op.Successors) Enqueue(workList, succ);
                }
            }
        }
        else
        {
            // TODO
            throw new NotImplementedException();
        }

        return result.ToImmutable();
    }
}
