using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.OptimizingIr;

/// <summary>
/// Read-only interface of an instruction.
/// </summary>
internal interface IInstruction
{
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
}
