using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// An instruction that produces a result in a register.
/// </summary>
internal interface IValueInstruction : IInstruction
{
    /// <summary>
    /// The register to store the result at.
    /// </summary>
    public Register Target { get; }
}
