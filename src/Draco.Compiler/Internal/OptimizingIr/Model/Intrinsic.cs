using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A compiler intrinsic.
/// </summary>
/// <param name="Symbol">The corresponding intrinsic symbol.</param>
internal readonly record struct Intrinsic(Symbol Symbol) : IOperand
{
    public override string ToString() => this.ToOperandString();
    public string ToOperandString() => $"[intrinsic {this.Symbol.Name}]";
}
