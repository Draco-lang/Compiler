using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.DracoIr.Passes;

/// <summary>
/// We simply remove basic blocks that are never referenced.
/// </summary>
internal sealed class RemoveUntargetedBlocks : IGlobalPass
{
    public bool Matches(IReadOnlyProcecude procedure) => true;
    public void Pass(Procedure procedure)
    {
        var referenced = GraphTraversal.DepthFirst(
            start: procedure.Entry,
            getNeighbors: GetNeighbors).ToHashSet();
        for (var i = 0; i < procedure.BasicBlocks.Count;)
        {
            if (referenced.Contains(procedure.BasicBlocks[i])) ++i;
            else procedure.BasicBlocks.RemoveAt(i);
        }
    }

    private static IEnumerable<BasicBlock> GetNeighbors(BasicBlock basicBlock)
    {
        if (basicBlock.Instructions.Count == 0) yield break;
        var lastInstr = basicBlock.Instructions[^1];
        if (lastInstr.Kind == InstructionKind.Jmp)
        {
            yield return lastInstr.GetOperandAt<BasicBlock>(0);
        }
        else if (lastInstr.Kind == InstructionKind.JmpIf)
        {
            yield return lastInstr.GetOperandAt<BasicBlock>(1);
            yield return lastInstr.GetOperandAt<BasicBlock>(2);
        }
    }
}
