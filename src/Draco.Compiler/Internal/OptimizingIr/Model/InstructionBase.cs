using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A mutable base class for <see cref="IInstruction"/> implementations.
/// </summary>
internal abstract class InstructionBase : IInstruction
{
    public BasicBlock BasicBlock { get; set; } = null!;
    IBasicBlock IInstruction.BasicBlock => this.BasicBlock;
    public InstructionBase? Prev { get; set; }
    IInstruction? IInstruction.Prev => this.Prev;
    public InstructionBase? Next { get; set; }
    IInstruction? IInstruction.Next => this.Next;
    public virtual bool IsBranch => false;
}
