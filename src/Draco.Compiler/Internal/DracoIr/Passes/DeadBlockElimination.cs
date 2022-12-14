using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.DracoIr.Passes;

/// <summary>
/// We simply remove basic blocks that are never referenced.
/// </summary>
internal static class DeadBlockElimination
{
    public static IOptimizationPass Instance { get; } = OptimizationPass.Procedure(procedure =>
    {
        var origCount = procedure.BasicBlocks.Count;
        var referenced = GraphTraversal.DepthFirst(
            start: procedure.Entry,
            getNeighbors: GetNeighbors).ToHashSet();
        for (var i = 0; i < procedure.BasicBlocks.Count;)
        {
            if (referenced.Contains(procedure.BasicBlocks[i])) ++i;
            else procedure.BasicBlocks.RemoveAt(i);
        }
        return origCount != procedure.BasicBlocks.Count;
    });

    private static IEnumerable<BasicBlock> GetNeighbors(BasicBlock basicBlock)
    {
        if (basicBlock.Instructions.Count == 0) yield break;
        var lastInstr = basicBlock.Instructions[^1];
        if (lastInstr.Kind == InstructionKind.Jmp)
        {
            yield return lastInstr[0].AsMutableBlock();
        }
        else if (lastInstr.Kind == InstructionKind.JmpIf)
        {
            yield return lastInstr[1].AsMutableBlock();
            yield return lastInstr[2].AsMutableBlock();
        }
    }
}
