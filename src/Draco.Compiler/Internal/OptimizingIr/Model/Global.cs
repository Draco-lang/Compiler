using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A global value that can be read from and written to.
/// </summary>
/// <param name="Symbol">The corresponding global symbol.</param>
internal readonly record struct Global(GlobalSymbol Symbol) : IOperand
{
    /// <summary>
    /// An optional name of this global.
    /// </summary>
    public string Name => this.Symbol.Name;

    /// <summary>
    /// The type this global holds.
    /// </summary>
    public TypeSymbol Type => this.Symbol.Type;

    public override string ToString() => $"global {this.ToOperandString()}: {this.Type}";
    public string ToOperandString() => this.Name;
}
