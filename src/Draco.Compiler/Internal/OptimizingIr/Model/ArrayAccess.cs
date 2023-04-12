using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Represents an array access.
/// </summary>
/// <param name="Array">The accessed array.</param>
/// <param name="Indices">The access indices.</param>
internal readonly record struct ArrayAccess(IOperand Array, ImmutableArray<IOperand> Indices) : IOperand
{
    public TypeSymbol Type => ((ArrayTypeSymbol)this.Array.Type!).ElementType;

    public override string ToString() => this.ToOperandString();
    public string ToOperandString() =>
        $"{this.Array.ToOperandString()}[{string.Join(", ", this.Indices.Select(i => i.ToOperandString()))}]";
}
