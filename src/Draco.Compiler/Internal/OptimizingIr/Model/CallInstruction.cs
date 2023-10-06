using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A procedure call.
/// </summary>
internal sealed class CallInstruction : InstructionBase, IValueInstruction
{
    public Register Target { get; set; }

    /// <summary>
    /// The called procedure.
    /// </summary>
    public FunctionSymbol Procedure { get; set; }

    /// <summary>
    /// The arguments that are passed to the procedure.
    /// </summary>
    public IList<IOperand> Arguments { get; set; } = new List<IOperand>();

    public CallInstruction(Register target, FunctionSymbol procedure, IEnumerable<IOperand> arguments)
    {
        this.Target = target;
        this.Procedure = procedure;
        this.Arguments = arguments.ToList();
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := call [{this.Procedure.FullName}]({string.Join(", ", this.Arguments.Select(a => a.ToOperandString()))})";

    public override CallInstruction Clone() => new(this.Target, this.Procedure, this.Arguments);
}
