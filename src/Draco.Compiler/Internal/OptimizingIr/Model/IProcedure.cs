using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Read-only interface of a procedure.
/// </summary>
internal interface IProcedure
{
    /// <summary>
    /// The symbol that corresponds to this procedure.
    /// </summary>
    public FunctionSymbol Symbol { get; }

    /// <summary>
    /// The assembly this procedure is defined in.
    /// </summary>
    public IAssembly Assembly { get; }

    /// <summary>
    /// The entry basic block of this procedure.
    /// </summary>
    public IBasicBlock Entry { get; }

    /// <summary>
    /// All basic blocks within this procedure.
    /// </summary>
    public IEnumerable<IBasicBlock> BasicBlocks { get; }
}
