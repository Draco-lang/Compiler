using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Represents the default value of a type.
/// </summary>
/// <param name="Type">The type the default value corresponds to.</param>
internal readonly record struct DefaultValue(TypeSymbol Type) : IOperand
{
    public string ToOperandString() => $"default({this.Type})";
}
