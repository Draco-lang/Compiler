using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// An immutable register for temporary-value computations.
/// </summary>
/// <param name="Type">The type this register holds.</param>
/// <param name="Index">The register index.</param>
internal readonly record struct Register(Type Type, int Index) : IOperand
{
    public override string ToString() => this.ToOperandString();
    public string ToOperandString() => $"r{this.Index}";
}
