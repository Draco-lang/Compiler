using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.OptimizingIr;

/// <summary>
/// Utility to reorder and annotate the register-based IR code to have better codegen on the CIL stack-machine.
/// </summary>
internal sealed class Stackifier
{
    private static ImmutableDictionary<Register, int> CountRegisterUses(IEnumerable<IInstruction> instructions)
    {
        var registerUses = ImmutableDictionary.CreateBuilder<Register, int>();
        // Definition sites initialize to 0
        foreach (var instruction in instructions.OfType<IValueInstruction>()) registerUses.Add(instruction.Target, 0);
        // Count uses
        foreach (var reg in instructions.SelectMany(instr => instr.Operands).OfType<Register>())
        {
            ++registerUses[reg];
        }
        return registerUses.ToImmutable();
    }

    private readonly IProcedure procedure;
    private readonly ImmutableDictionary<Register, int> registerUses;

    public Stackifier(IProcedure procedure)
    {
        this.procedure = procedure;
        var instructions = procedure.BasicBlocks.Values.SelectMany(bb => bb.Instructions);
        // Count the number of register uses
        this.registerUses = CountRegisterUses(instructions);
    }

    /// <summary>
    /// Stackifies the given basic block.
    /// </summary>
    /// <param name="basicBlock">The basic block to stackify.</param>
    /// <returns>The index of the instructions that has to leak onto registers.</returns>
    public ImmutableArray<int> Stackify(IBasicBlock basicBlock)
    {
        if (basicBlock.Procedure != this.procedure)
        {
            throw new ArgumentException("only basic-blocks belonging to the specified procedure can be stackified", nameof(basicBlock));
        }

        var instructions = basicBlock.Instructions.ToImmutableArray();
        var commitPoints = ImmutableArray.CreateBuilder<int>();
        var index = basicBlock.InstructionCount;
        while (index > 0)
        {
            --index;
            // This is a commit-point
            commitPoints.Add(index);
            // Recover the longest tree backwards
            this.RecoverTree(instructions, ref index);
        }

        // Reverse for convenience
        commitPoints.Reverse();
        return commitPoints.ToImmutable();
    }

    private bool RecoverTree(ImmutableArray<IInstruction> instructions, ref int offset)
    {
        var instr = instructions[offset];
        var stopped = false;

        foreach (var op in instr.Operands.Reverse())
        {
            // Not a register, pushed some other way, does not break tree
            if (op is not Register reg) continue;

            // If we have a single-use register as a result immediately before this instruction,
            // all good, part of the tree
            if (!stopped
             && this.registerUses[reg] == 1
             && instructions[offset - 1] is IValueInstruction valueInstr
             && valueInstr.Target == reg)
            {
                --offset;
                var childStopped = this.RecoverTree(instructions, ref offset);
                // If child recovery broke the tree, we break too
                if (childStopped) stopped = true;
                continue;
            }

            // Match failure, need to break the tree
            stopped = true;
        }

        return stopped;
    }
}
