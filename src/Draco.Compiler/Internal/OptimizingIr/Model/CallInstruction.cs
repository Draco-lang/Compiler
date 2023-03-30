using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A procedure call.
/// </summary>
internal sealed class CallInstruction : InstructionBase
{
    public override IEnumerable<IOperand> Operands => new[] { this.Target, this.Procedure }
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
    /// The arguments that are passed to the procedure.
    /// </summary>
    public IList<IOperand> Arguments { get; set; } = new List<IOperand>();

    public CallInstruction(Register target, IOperand procedure, IList<IOperand> arguments)
    {
        this.Target = target;
        this.Procedure = procedure;
        this.Arguments = arguments;
    }

    public override string ToString() =>
        $"{this.Target} := call {this.Procedure}({string.Join(", ", this.Arguments)})";
}
