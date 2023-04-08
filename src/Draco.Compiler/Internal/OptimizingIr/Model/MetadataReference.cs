using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// An element from metadata.
/// </summary>
/// <param name="Symbol">The symbol referencing the metadata.</param>
internal readonly record struct MetadataReference(Symbol Symbol) : IOperand
{
    public Type? Type => (this.Symbol as ITypedSymbol)?.Type;

    public override string ToString() => this.ToOperandString();
    public string ToOperandString() => $"[metadata {this.Symbol.FullName}]";
}
