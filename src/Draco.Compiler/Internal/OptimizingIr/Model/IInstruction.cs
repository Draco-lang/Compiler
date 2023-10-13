using System.Collections.Generic;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Read-only interface of an instruction.
/// </summary>
internal interface IInstruction
{
    /// <summary>
    /// The keyword notating the instruction.
    /// </summary>
    public string InstructionKeyword { get; }

    /// <summary>
    /// The basic block this instruction is a part of.
    /// </summary>
    public IBasicBlock BasicBlock { get; }

    /// <summary>
    /// The previous instruction in the same basic-block.
    /// </summary>
    public IInstruction? Prev { get; }

    /// <summary>
    /// The next instruction in the same basic-block.
    /// </summary>
    public IInstruction? Next { get; }

    /// <summary>
    /// True, if this is some kind of branching instruction, modifying control-flow.
    /// </summary>
    public bool IsBranch { get; }

    /// <summary>
    /// True, if this instruction is valid in a detached block state, which is true for certain debugger metadata.
    /// </summary>
    public bool IsValidInUnreachableContext { get; }

    /// <summary>
    /// The jump targets for this instruction.
    /// </summary>
    public IEnumerable<IBasicBlock> JumpTargets { get; }

    /// <summary>
    /// The static operands of this instruction that are not runtime values.
    /// </summary>
    public IEnumerable<Symbol> StaticOperands { get; }

    /// <summary>
    /// The input operands of this instruction.
    /// </summary>
    public IEnumerable<IOperand> Operands { get; }

    /// <summary>
    /// Clones this instruction.
    /// </summary>
    /// <returns>The clone of this instruction not associated to any block.</returns>
    public IInstruction Clone();
}
