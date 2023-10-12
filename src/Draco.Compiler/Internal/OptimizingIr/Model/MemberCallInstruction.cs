using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A procedure call on a member.
/// </summary>
internal sealed class MemberCallInstruction : InstructionBase, IValueInstruction
{
    public string InstructionKeyword => "membercall";

    public Register Target { get; set; }

    /// <summary>
    /// The called procedure.
    /// </summary>
    public FunctionSymbol Procedure { get; set; }

    /// <summary>
    /// The receiver the method is called on.
    /// </summary>
    public IOperand Receiver { get; set; }

    /// <summary>
    /// The arguments that are passed to the procedure.
    /// </summary>
    public IList<IOperand> Arguments { get; set; } = new List<IOperand>();

    public override IEnumerable<IOperand> Operands => this.Arguments.Prepend(this.Receiver);

    public MemberCallInstruction(Register target, FunctionSymbol procedure, IOperand receiver, IEnumerable<IOperand> arguments)
    {
        this.Target = target;
        this.Procedure = procedure;
        this.Receiver = receiver;
        this.Arguments = arguments.ToList();
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.InstructionKeyword} {this.Receiver.ToOperandString()}.[{this.Procedure.FullName}]({string.Join(", ", this.Arguments.Select(a => a.ToOperandString()))})";

    public override MemberCallInstruction Clone() => new(this.Target, this.Procedure, this.Receiver, this.Arguments);
}
