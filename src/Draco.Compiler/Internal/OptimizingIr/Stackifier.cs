using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

    /// <summary>
    /// A dictionary for the number of register uses for each register.
    /// </summary>
    public ImmutableDictionary<Register, int> RegisterUses { get; }

    private readonly IProcedure procedure;

    public Stackifier(IProcedure procedure)
    {
        this.procedure = procedure;
        var instructions = procedure.BasicBlocks.Values.SelectMany(bb => bb.Instructions);
        this.RegisterUses = CountRegisterUses(instructions);
    }

    /// <summary>
    /// Stackifies the given basic block.
    /// </summary>
    /// <param name="basicBlock">The basic block to stackify.</param>
    /// <returns>The new, stackified array of instructions.</returns>
    public ImmutableArray<IInstruction> Stackify(IBasicBlock basicBlock)
    {
        if (!this.procedure.BasicBlocks.Values.Contains(basicBlock))
        {
            throw new ArgumentOutOfRangeException(nameof(basicBlock));
        }

        var result = ImmutableArray.CreateBuilder<IInstruction>();

        var instr = basicBlock.LastInstruction;
        while (instr is not null)
        {
            // Recover the longest tree backwards
            var (tree, _) = this.RecoverTree(ref instr);
            // Add result
            result.Add(tree);
            // Step back
            instr = instr.Prev;
        }

        result.Reverse();
        return result.ToImmutable();
    }

    private (TreeInstruction Tree, bool Stopped) RecoverTree(ref IInstruction instrIterator)
    {
        var instr = instrIterator;
        var children = ImmutableArray.CreateBuilder<IOperand>();
        var stopped = false;

        foreach (var op in instr.Operands.Reverse())
        {
            // Not a register, pushed some other way, does not break tree
            if (op is not Register reg)
            {
                children.Add(op);
                continue;
            }

            // If we have a single-use register as a result immediately before this instruction,
            // all good, part of the tree
            if (!stopped
             && this.RegisterUses[reg] == 1
             && instrIterator.Prev is IValueInstruction valueInstr
             && valueInstr.Target == reg)
            {
                instrIterator = instrIterator.Prev;
                var (childTree, childStopped) = this.RecoverTree(ref instrIterator);
                children.Add(childTree);
                // If child recovery broke the tree, we break too
                if (childStopped) stopped = true;
                continue;
            }

            // Match failure, need to break the tree
            children.Add(op);
            stopped = true;
        }

        children.Reverse();
        var tree = new TreeInstruction(instr, children.ToImmutable());
        return (tree, stopped);
    }
}
