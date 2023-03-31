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
/// <param name="Symbol">The corresponding local symbol.</param>
/// <param name="Index">The index of this local to help naming.</param>
internal readonly record struct Local(LocalSymbol Symbol, int Index) : IOperand
{
    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append($"{this.ToOperandString()}: {this.Symbol.Type}");
        if (!string.IsNullOrWhiteSpace(this.Symbol.Name)) result.Append($" ; {this.Symbol.Name}");
        return result.ToString();
    }
    public string ToOperandString() => $"loc{this.Index}";
}
