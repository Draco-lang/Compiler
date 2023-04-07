using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Types;

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
