using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A read-only parameter value defined by a procedure.
/// </summary>
/// <param name="Symbol">The corresponding parameter symbol.</param>
/// <param name="Index">The index of the parameter.</param>
internal readonly record struct Parameter(ParameterSymbol Symbol, int Index) : IOperand
{
    public override string ToString() => $"{this.ToOperandString()}: {this.Symbol.Type}";
    public string ToOperandString() => this.Symbol.Name;
}
