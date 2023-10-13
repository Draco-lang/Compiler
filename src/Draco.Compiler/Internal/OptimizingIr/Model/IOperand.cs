using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// The interface of all operands that an instruction can hold.
/// </summary>
internal interface IOperand
{
    /// <summary>
    /// The type of this operand.
    /// </summary>
    public TypeSymbol Type { get; }

    /// <summary>
    /// Returns a string representation of the operand.
    ///
    /// The reason that <see cref="object.ToString"/> is not used is because some entities are printed
    /// with <see cref="object.ToString"/>, when they are not operands, for example procedures and basic blocks.
    /// </summary>
    /// <returns>The string representation of this operand.</returns>
    public string ToOperandString();
}
