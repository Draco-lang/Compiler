using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Represents no value.
/// </summary>
internal readonly record struct Void : IOperand
{
    public override string ToString() => "void";
    public string ToOperandString() => this.ToString();
}
