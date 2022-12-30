using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.DracoIr.Passes;

// TODO: Buggy, inlining causes duplicate register assignments
/// <summary>
/// An optimization pass that inlines small unconditional jump targets.
/// For example, the following
///
/// label bb_1:
///   ...
///   jmp bb_2
/// label bb_2:
///   ; very few instructions
///   ; some branch or return
///
/// can be inlined to
///
/// label bb_1:
///   ...
///   ; very few instructions
///   ; some branch or return
/// 
/// </summary>
internal static class InlineSmallBlocks
{
    public static IOptimizationPass Instance { get; } = OptimizationPass.BasicBlock(
        filter: bb => bb.Instructions[^1].Kind == InstructionKind.Jmp,
        passDelegate: Apply);

    private static readonly int FewInstructionsTreshold = 3;

    private static bool Apply(BasicBlock basicBlock)
    {
        var target = basicBlock.Instructions[^1][0].AsBlock();
        if (target == basicBlock) return false;
        if (target.Instructions.Count > FewInstructionsTreshold) return false;

        // It really is just a few instructions, inline
        basicBlock.Instructions.RemoveAt(basicBlock.Instructions.Count - 1);
        foreach (var instr in target.Instructions) basicBlock.Instructions.Add((Instruction)instr);
        return true;
    }
}
