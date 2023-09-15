using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// References the address of a local or global.
/// </summary>
/// <param name="Symbol">The symbol to reference the address of.</param>
internal readonly record struct Address(IOperand Operand) : IOperand
{
    public TypeSymbol? Type => null;

    public string ToOperandString() => $"(ref {this.Operand.ToOperandString()})";
}
