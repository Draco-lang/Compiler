using System.Linq;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.OptimizingIr.Passes;

/// <summary>
/// We simply remove basic blocks that are never referenced.
/// </summary>
internal static class DeadBlockElimination
{
    public static IPass Instance { get; } = PassFactory.Procedure(procedure =>
    {
        var referenced = GraphTraversal.DepthFirst(
            start: procedure.Entry,
            getNeighbors: bb => bb.Successors);
        var notReferenced = procedure.BasicBlocks.Values
            .Except(referenced)
            .ToList();
        var changed = false;
        foreach (var bb in notReferenced) changed = procedure.RemoveBasicBlock(bb) || changed;
        return changed;
    });
}
