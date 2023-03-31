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
internal readonly record struct Local(LocalSymbol Symbol) : IOperand
{
    public override string ToString() => $"{this.ToOperandString()}: {this.Symbol.Type}";
    public string ToOperandString() => this.Symbol.Name;
}
