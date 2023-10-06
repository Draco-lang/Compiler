using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// An object instantiation.
/// </summary>
internal sealed class NewObjectInstruction : InstructionBase, IValueInstruction
{
    public Register Target { get; set; }

    /// <summary>
    /// The called constructor.
    /// </summary>
    public FunctionSymbol Constructor { get; set; }

    /// <summary>
    /// The arguments that are passed to the constructor.
    /// </summary>
    public IList<IOperand> Arguments { get; set; } = new List<IOperand>();

    public NewObjectInstruction(Register target, FunctionSymbol constructor, IEnumerable<IOperand> arguments)
    {
        this.Target = target;
        this.Constructor = constructor;
        this.Arguments = arguments.ToList();
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := new [{this.Constructor.FullName}]({string.Join(", ", this.Arguments.Select(a => a.ToOperandString()))})";

    public override NewObjectInstruction Clone() => new(this.Target, this.Constructor, this.Arguments);
}
