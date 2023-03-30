using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A local value that can be read from and written to.
/// </summary>
/// <param name="Symbol">The corresponding local symbol, if any.</param>
/// <param name="Type">The type of this local.</param>
/// <param name="Index">The index of this local.</param>
internal readonly record struct Local(LocalSymbol? Symbol, Type Type, int Index) : IOperand
{
    public Local(LocalSymbol symbol, int index)
        : this(symbol, symbol.Type, index)
    {
    }

    public Local(Type type, int index)
        : this(null, type, index)
    {
    }

    public override string ToString() => $"{this.ToOperandString()}: {this.Type}";

    public string ToOperandString() => this.Symbol is null
        ? $"loc_{this.Index}"
        : $"{this.Symbol.Name}_{this.Index}";
}
