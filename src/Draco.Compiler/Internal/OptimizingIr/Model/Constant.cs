using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A constant value.
/// </summary>
/// <param name="Value">The constant value.</param>
internal readonly record struct Constant(object? Value) : IOperand
{
    public override string ToString() => this.Value?.ToString() ?? "null";
    public string ToOperandString() => this.ToString();
}
