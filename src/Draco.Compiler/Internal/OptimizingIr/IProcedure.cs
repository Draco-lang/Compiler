using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.OptimizingIr;

/// <summary>
/// Read-only interface of a procedure.
/// </summary>
internal interface IProcedure
{
    /// <summary>
    /// The assembly this procedure is defined in.
    /// </summary>
    public IAssembly Assembly { get; }

    /// <summary>
    /// The entry basic block of this procedure.
    /// </summary>
    public IBasicBlock Entry { get; }
}
