using System.Collections.Generic;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Read-only interface of a program.
/// </summary>
internal interface IAssembly
{
    /// <summary>
    /// The root module of this assembly.
    /// </summary>
    public IModule RootModule { get; }

    /// <summary>
    /// The name of this assembly.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The entry point of this assembly.
    /// </summary>
    public IProcedure? EntryPoint { get; }
}
