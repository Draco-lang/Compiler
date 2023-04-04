using System.Collections.Generic;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Utilities;

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
    public virtual bool IsValidInUnreachableContext => false;
    public virtual IEnumerable<BasicBlock> JumpTargets => Enumerable.Empty<BasicBlock>();
    IEnumerable<IBasicBlock> IInstruction.JumpTargets => this.JumpTargets;
    public virtual IEnumerable<IOperand> Operands => Enumerable.Empty<IOperand>();

    public override string ToString()
    {
        var result = new StringBuilder();

        // Infer a good operand name
        var name = this.GetType().Name;
        if (name.EndsWith("Instruction")) name = name[..^11];
        name = StringUtils.ToSnakeCase(name);

        // Append it
        result.Append(name);

        // If we have operands, add a space, then write them comma-separated
        if (this.Operands.Any())
        {
            result.Append(' ');
            result.AppendJoin(", ", this.Operands.Select(o => o.ToOperandString()));
        }

        // Done
        return result.ToString();
    }

    public abstract IInstruction Clone();
}
