using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A constant value.
/// </summary>
/// <param name="Value">The constant value.</param>
internal readonly record struct Constant(object? Value) : IOperand
{
    public TypeSymbol? Type => this.Value switch
    {
        int => IntrinsicSymbols.Int32,
        bool => IntrinsicSymbols.Bool,
        string => IntrinsicSymbols.String,
        _ => throw new System.InvalidOperationException(),
    };

    public override string ToString() => this.ToOperandString();
    public string ToOperandString() => this.Value switch
    {
        string s => $"\"{StringUtils.Unescape(s)}\"",
        bool b => b ? "true" : "false",
        _ => this.Value?.ToString() ?? "null",
    };
}
