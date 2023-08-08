using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A constant value.
/// </summary>
/// <param name="Value">The constant value.</param>
/// <param name="Type">The type of the constant.</param>
internal readonly record struct Constant(object? Value, TypeSymbol? Type) : IOperand
{
    public override string ToString() => this.ToOperandString();
    public string ToOperandString() => this.Value switch
    {
        string s => $"\"{StringUtils.Unescape(s)}\"",
        bool b => b ? "true" : "false",
        _ => this.Value?.ToString() ?? "null",
    };
}
