using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A constant value.
/// </summary>
/// <param name="Value">The constant value.</param>
internal readonly record struct Constant(object? Value) : IOperand
{
    public override string ToString() => this.ToOperandString();
    public string ToOperandString() => this.Value switch
    {
        string s => $"\"{StringUtils.Unescape(s)}\"",
        bool b => b ? "true" : "false",
        _ => this.Value?.ToString() ?? "null",
    };
}
