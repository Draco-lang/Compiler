using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Instructions;

/// <summary>
/// An object instantiation.
/// </summary>
internal sealed class NewObjectInstruction(
    Register target,
    FunctionSymbol constructor,
    IEnumerable<IOperand> arguments) : InstructionBase, IValueInstruction
{
    public override string InstructionKeyword => "newobject";

    public Register Target { get; set; } = target;

    /// <summary>
    /// The called constructor.
    /// </summary>
    public FunctionSymbol Constructor { get; set; } = constructor;

    /// <summary>
    /// The arguments that are passed to the constructor.
    /// </summary>
    public IList<IOperand> Arguments { get; set; } = arguments.ToList();

    public override IEnumerable<Symbol> StaticOperands => [this.Constructor];
    public override IEnumerable<IOperand> Operands => this.Arguments;

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := {this.InstructionKeyword} [{this.Constructor.FullName}]({string.Join(", ", this.Arguments.Select(a => a.ToOperandString()))})";

    public override NewObjectInstruction Clone() => new(this.Target, this.Constructor, this.Arguments);
}
