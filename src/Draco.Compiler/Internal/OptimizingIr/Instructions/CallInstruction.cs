using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Instructions;

/// <summary>
/// A procedure call.
/// </summary>
internal sealed class CallInstruction(
    Register target,
    FunctionSymbol procedure,
    IOperand? receiver,
    IEnumerable<IOperand> arguments) : InstructionBase, IValueInstruction
{
    public override string InstructionKeyword => "call";

    public Register Target { get; set; } = target;

    /// <summary>
    /// The called procedure.
    /// </summary>
    public FunctionSymbol Procedure { get; set; } = procedure;

    /// <summary>
    /// The receiver the method is called on.
    /// </summary>
    public IOperand? Receiver { get; set; } = receiver;

    /// <summary>
    /// The arguments that are passed to the procedure.
    /// </summary>
    public IList<IOperand> Arguments { get; set; } = arguments.ToList();

    public override IEnumerable<Symbol> StaticOperands => [this.Procedure];
    public override IEnumerable<IOperand> Operands => this.Receiver is null
        ? this.Arguments
        : this.Arguments.Prepend(this.Receiver);

    public override string ToString()
    {
        var target = this.Target.ToOperandString();
        var receiver = this.Receiver is null
            ? string.Empty
            : $"{this.Receiver.ToOperandString()}.";
        var args = string.Join(", ", this.Arguments.Select(a => a.ToOperandString()));
        return $"{target} := {this.InstructionKeyword} {receiver}[{this.Procedure.FullName}]({args})";
    }

    public override CallInstruction Clone() => new(this.Target, this.Procedure, this.Receiver, this.Arguments);
}
