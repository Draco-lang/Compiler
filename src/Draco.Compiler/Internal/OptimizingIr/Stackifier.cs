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
    /// <summary>
    /// Stackifies the given procedure without rearranging instructions.
    /// </summary>
    /// <param name="procedure">The procedure to stackify.</param>
    /// <returns>The registers that got stackified.</returns>
    public static ImmutableHashSet<Register> Stackify(IProcedure procedure)
    {
        var stackifier = new Stackifier(procedure);
        foreach (var bb in procedure.BasicBlocks.Values) stackifier.Stackify(bb);
        // Subtract to get stackified regs
        return stackifier.registerUses.Keys
            .Except(stackifier.savedRegisters)
            .ToImmutableHashSet();
    }

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
    private readonly HashSet<Register> savedRegisters = new();

    private Stackifier(IProcedure procedure)
    {
        this.procedure = procedure;
        var instructions = procedure.BasicBlocks.Values.SelectMany(bb => bb.Instructions);
        // Count the number of register uses
        this.registerUses = CountRegisterUses(instructions);
    }

    private void Stackify(IBasicBlock basicBlock)
    {
        var instr = basicBlock.LastInstruction;
        while (instr is not null)
        {
            // This instruction has to have its registers saved
            if (instr is IValueInstruction valueInstr) this.savedRegisters.Add(valueInstr.Target);
            // Recover the longest tree backwards
            this.RecoverTree(ref instr);
            // Step back
            instr = instr.Prev;
        }
    }

    private bool RecoverTree(ref IInstruction instrIterator)
    {
        var instr = instrIterator;
        var stopped = false;

        foreach (var op in instr.Operands.Reverse())
        {
            // Not a register, pushed some other way, does not break tree
            if (op is not Register reg) continue;

            // If we have a single-use register as a result immediately before this instruction,
            // all good, part of the tree
            if (!stopped
             && this.registerUses[reg] == 1
             && instrIterator.Prev is IValueInstruction valueInstr
             && valueInstr.Target == reg)
            {
                instrIterator = instrIterator.Prev;
                var childStopped = this.RecoverTree(ref instrIterator);
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
