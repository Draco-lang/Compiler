using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// An immutable register for temporary-value computations.
/// </summary>
/// <param name="Index">The register index.</param>
internal readonly record struct Register(int Index) : IOperand
{
    public override string ToString() => $"r{this.Index}";
}
