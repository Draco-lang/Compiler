using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Read-only interface of a basic-block.
/// </summary>
internal interface IBasicBlock
{
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
    /// All basic blocks that can come after this one.
    /// </summary>
    public IEnumerable<IBasicBlock> Successors { get; }
}
