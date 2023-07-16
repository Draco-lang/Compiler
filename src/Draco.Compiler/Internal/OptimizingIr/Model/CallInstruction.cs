using System.Collections.Generic;
using System.Linq;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A procedure call.
/// </summary>
internal sealed class CallInstruction : InstructionBase
{
    /// <summary>
    /// The register to write the call result to.
    /// </summary>
    public Register Target { get; set; }

    /// <summary>
    /// The called procedure.
    /// </summary>
    public IOperand Procedure { get; set; }

    /// <summary>
    /// The arguments that are passed to the procedure.
    /// </summary>
    public IList<IOperand> Arguments { get; set; } = new List<IOperand>();

    public CallInstruction(Register target, IOperand procedure, IEnumerable<IOperand> arguments)
    {
        this.Target = target;
        this.Procedure = procedure;
        this.Arguments = arguments.ToList();
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := call {this.Procedure.ToOperandString()}({string.Join(", ", this.Arguments.Select(a => a.ToOperandString()))})";

    public override CallInstruction Clone() => new(this.Target, this.Procedure, this.Arguments);
}
