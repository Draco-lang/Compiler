using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A global value that can be read from and written to.
/// </summary>
/// <param name="Symbol">The corresponding global symbol.</param>
internal readonly record struct Global(GlobalSymbol Symbol) : IOperand
{
    public override string ToString() => $"global {this.ToOperandString()}: {this.Symbol.Type}";
    public string ToOperandString() => this.Symbol.Name;
}
