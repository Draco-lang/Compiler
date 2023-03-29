using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.OptimizingIr;

/// <summary>
/// Read-only interface of a compilation unit.
/// </summary>
internal interface IAssembly
{
    /// <summary>
    /// The name of this assembly.
    /// </summary>
    public string Name { get; }
}
