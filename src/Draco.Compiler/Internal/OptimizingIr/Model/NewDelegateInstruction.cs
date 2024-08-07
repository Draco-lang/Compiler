using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A delegate instantiation.
/// </summary>
internal sealed class NewDelegateInstruction(
    Register target,
    IOperand? receiver,
    FunctionSymbol function,
    FunctionSymbol delegateConstructor) : InstructionBase, IValueInstruction
{
    public override string InstructionKeyword => "newdelegate";

    public Register Target { get; set; } = target;

    /// <summary>
    /// The optional receiver bound to the delegate.
    /// </summary>
    public IOperand? Receiver { get; set; } = receiver;

    /// <summary>
    /// The function being wrapped as a delegate.
    /// </summary>
    public FunctionSymbol Function { get; set; } = function;

    /// <summary>
    /// The delegate constructor.
    /// </summary>
    public FunctionSymbol DelegateConstructor { get; set; } = delegateConstructor;

    public override IEnumerable<Symbol> StaticOperands => [this.Function, this.DelegateConstructor];
    public override IEnumerable<IOperand> Operands => this.Receiver is null ? [] : [this.Receiver];

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.InstructionKeyword} {this.Receiver?.ToOperandString() ?? "null"}.[{this.Function.FullName}] with {this.DelegateConstructor.FullName}";

    public override NewDelegateInstruction Clone() => new(this.Target, this.Receiver, this.Function, this.DelegateConstructor);
}
