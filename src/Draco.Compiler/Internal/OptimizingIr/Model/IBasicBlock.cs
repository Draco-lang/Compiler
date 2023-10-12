using System.Collections.Generic;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Read-only interface of a basic-block.
/// </summary>
internal interface IBasicBlock
{
    /// <summary>
    /// The corresponding label.
    /// </summary>
    public LabelSymbol Symbol { get; }

    /// <summary>
    /// The procedure the block was defined in.
    /// </summary>
    public IProcedure Procedure { get; }

    /// <summary>
    /// The first instruction within this block.
    /// </summary>
    public IInstruction FirstInstruction { get; }

    /// <summary>
    /// The last instruction within this block.
    /// </summary>
    public IInstruction LastInstruction { get; }

    /// <summary>
    /// All instructions within this basic block.
    /// </summary>
    public IEnumerable<IInstruction> Instructions { get; }

    /// <summary>
    /// The number of instructions in this block.
    /// </summary>
    public int InstructionCount { get; }

    /// <summary>
    /// All basic blocks that can come after this one.
    /// </summary>
    public IEnumerable<IBasicBlock> Successors { get; }
}
