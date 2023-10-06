namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// An array length query.
/// </summary>
internal sealed class ArrayLengthInstruction : InstructionBase, IValueInstruction
{
    public Register Target { get; set; }

    /// <summary>
    /// The array to get the length of.
    /// </summary>
    public IOperand Array { get; set; }

    public ArrayLengthInstruction(Register target, IOperand array)
    {
        this.Target = target;
        this.Array = array;
    }

    public override string ToString() =>
        $"{this.Target.ToOperandString()} := length {this.Array.ToOperandString()}";

    public override ArrayLengthInstruction Clone() => new(this.Target, this.Array);
}
