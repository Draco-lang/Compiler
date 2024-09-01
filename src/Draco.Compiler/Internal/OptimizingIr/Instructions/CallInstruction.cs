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
    IEnumerable<IOperand> arguments) : InstructionBase, IValueInstruction
{
    public override string InstructionKeyword => "call";

    public Register Target { get; set; } = target;

    /// <summary>
    /// The called procedure.
    /// </summary>
    public FunctionSymbol Procedure { get; set; } = procedure;

    /// <summary>
    /// The arguments that are passed to the procedure.
    /// </summary>
    public IList<IOperand> Arguments { get; set; } = arguments.ToList();

    public override IEnumerable<Symbol> StaticOperands => [this.Procedure];
    public override IEnumerable<IOperand> Operands => this.Arguments;

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.InstructionKeyword} [{this.Procedure.FullName}]({string.Join(", ", this.Arguments.Select(a => a.ToOperandString()))})";

    public override CallInstruction Clone() => new(this.Target, this.Procedure, this.Arguments);
}
