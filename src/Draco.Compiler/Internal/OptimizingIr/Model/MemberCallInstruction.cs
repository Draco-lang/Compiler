using System.Collections.Generic;
using System.Linq;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A procedure call on a member.
/// </summary>
internal sealed class MemberCallInstruction : InstructionBase
{
    public override IEnumerable<IOperand> Operands => new[] { this.Target, this.Procedure, this.Receiver }
        .Concat(this.Arguments);

    /// <summary>
    /// The register to write the call result to.
    /// </summary>
    public Register Target { get; set; }

    /// <summary>
    /// The called procedure.
    /// </summary>
    public IOperand Procedure { get; set; }

    /// <summary>
    /// The receiver the method is called on.
    /// </summary>
    public IOperand Receiver { get; set; }

    /// <summary>
    /// The arguments that are passed to the procedure.
    /// </summary>
    public IList<IOperand> Arguments { get; set; } = new List<IOperand>();

    public MemberCallInstruction(Register target, IOperand procedure, IOperand receiver, IEnumerable<IOperand> arguments)
    {
        this.Target = target;
        this.Procedure = procedure;
        this.Receiver = receiver;
        this.Arguments = arguments.ToList();
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := call {this.Receiver.ToOperandString()}.{this.Procedure.ToOperandString()}({string.Join(", ", this.Arguments.Select(a => a.ToOperandString()))})";

    public override MemberCallInstruction Clone() => new(this.Target, this.Procedure, this.Receiver, this.Arguments);
}
