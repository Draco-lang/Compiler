using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.DracoIr.Passes;

/// <summary>
/// Jump threading can happen, when the target of a jump instruction is a sole jump instruction itself.
///
/// For example:
///
/// label bb_1:
///     jmp bb_2
/// label bb_2:
///     jmp bb_3
///
/// Can be rewritten as:
/// 
/// label bb_1:
///     jmp bb_3
///
/// There are three cases we care about:
///  1) An unconditional jump targets an unconditional jump, in which case we simply replace the jump target
///  2) An unconditional jump targets a conditional jump, in which case we inline the conditional jump
///  3) A conditional jump branch (either) targets an unconditional jump, in which case we can inline the proper target
/// </summary>
internal static class JumpThreading
{
    public static IOptimizationPass Instance { get; } = OptimizationPass.Instruction(
        filter: instruction => instruction.Kind is InstructionKind.Jmp or InstructionKind.JmpIf,
        passDelegate: Apply);

    private static bool Apply(ref Instruction instruction)
    {
        if (instruction.Kind == InstructionKind.Jmp)
        {
            var target = instruction[0].AsMutableBlock();
            if (!IsSingleInstruction(target, out var targetIns)) return false;
            if (targetIns.Kind == InstructionKind.Jmp)
            {
                // Case 1
                var targetBlock = targetIns[0].AsBlock();
                instruction[0] = targetBlock;
                return true;
            }
            else if (targetIns.Kind == InstructionKind.JmpIf)
            {
                // Case 2
                instruction = targetIns;
                return true;
            }
            return false;
        }
        else
        {
            Debug.Assert(instruction.Kind == InstructionKind.JmpIf);

            var thenTarget = instruction[1].AsMutableBlock();
            var elsTarget = instruction[2].AsMutableBlock();
            if (IsSingleInstruction(thenTarget, out var thenTargetIns) && thenTargetIns.Kind == InstructionKind.Jmp)
            {
                // Case 3 on then branch
                var thenTargetBlock = thenTargetIns[0].AsBlock();
                instruction[1] = thenTargetBlock;
                return true;
            }
            if (IsSingleInstruction(elsTarget, out var elsTargetIns) && elsTargetIns.Kind == InstructionKind.Jmp)
            {
                // Case 3 on else branch
                var elsTargetBlock = elsTargetIns[0].AsBlock();
                instruction[2] = elsTargetBlock;
                return true;
            }
            return false;
        }
    }

    private static bool IsSingleInstruction(BasicBlock bb, [MaybeNullWhen(false)] out Instruction instr)
    {
        if (bb.Instructions.Count == 1)
        {
            instr = bb.Instructions[0];
            return true;
        }
        else
        {
            instr = null;
            return false;
        }
    }
}
