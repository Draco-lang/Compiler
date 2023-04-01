using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.OptimizingIr.Passes;

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
    public static IPass Instance { get; } = PassFactory.Instruction(
        filter: instruction => instruction.IsBranch,
        passDelegate: Apply);

    private static bool Apply(IInstruction instruction)
    {
        var bb = (BasicBlock)instruction.BasicBlock;
        if (instruction is JumpInstruction jump)
        {
            var target = jump.Target;
            if (!IsSingleInstruction(target, out var targetIns)) return false;
            if (targetIns is JumpInstruction targetJump)
            {
                // Case 1
                jump.Target = targetJump.Target;
                return true;
            }
            else if (targetIns is BranchInstruction targetBranch)
            {
                // Case 2
                // We replace the instruction
                bb.InsertBefore(jump, targetBranch.Clone());
                bb.Remove(jump);
                return true;
            }
        }
        else if (instruction is BranchInstruction branch)
        {
            var thenTarget = branch.Then;
            var elsTarget = branch.Else;
            if (IsSingleInstruction(thenTarget, out var thenTargetIns) && thenTargetIns is JumpInstruction thenJump)
            {
                // Case 3 on then branch
                branch.Then = thenJump.Target;
                return true;
            }
            if (IsSingleInstruction(elsTarget, out var elsTargetIns) && elsTargetIns is JumpInstruction elseJump)
            {
                // Case 3 on else branch
                branch.Else = elseJump.Target;
                return true;
            }
        }
        return false;
    }

    private static bool IsSingleInstruction(BasicBlock bb, [MaybeNullWhen(false)] out IInstruction instr)
    {
        if (bb.InstructionCount == 1)
        {
            instr = bb.Instructions.First();
            return true;
        }
        else
        {
            instr = null;
            return false;
        }
    }
}
