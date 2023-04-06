using System.Collections.Generic;
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
    /// The globals within this assembly.
    /// </summary>
    public IReadOnlyDictionary<GlobalSymbol, Global> Globals { get; }

    /// <summary>
    /// The procedure performing global initialization.
    /// </summary>
    public IProcedure GlobalInitializer { get; }

    /// <summary>
    /// The compiled procedures within this assembly.
    /// </summary>
    public IReadOnlyDictionary<FunctionSymbol, IProcedure> Procedures { get; }

    /// <summary>
    /// The entry point of this assembly.
    /// </summary>
    public IProcedure? EntryPoint { get; }
}
