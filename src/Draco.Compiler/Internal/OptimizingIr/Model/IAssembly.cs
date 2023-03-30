using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Read-only interface of a compilation unit.
/// </summary>
internal interface IAssembly
{
    /// <summary>
    /// The symbol that corresponds to this compilation unit.
    /// </summary>
    public ModuleSymbol Symbol { get; }

    /// <summary>
    /// The name of this assembly.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The compiled procedures within this assembly.
    /// </summary>
    public IReadOnlyDictionary<FunctionSymbol, IProcedure> Procedures { get; }
}
