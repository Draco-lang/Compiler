using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A read-only parameter value defined by a procedure.
/// </summary>
/// <param name="Symbol">The corresponding parameter symbol.</param>
/// <param name="Index">The index of the parameter.</param>
internal readonly record struct Parameter(ParameterSymbol Symbol, int Index) : IOperand
{
    /// <summary>
    /// An optional name of this parameter.
    /// </summary>
    public string Name => this.Symbol.Name;

    /// <summary>
    /// The type this parameter holds.
    /// </summary>
    public Type Type => this.Symbol.Type;

    public override string ToString() => $"{this.ToOperandString()}: {this.Type}";
    public string ToOperandString() => this.Name;

    public bool Equals(Parameter other) => this.Symbol == other.Symbol;
    public override int GetHashCode() => this.Symbol.GetHashCode();
}
