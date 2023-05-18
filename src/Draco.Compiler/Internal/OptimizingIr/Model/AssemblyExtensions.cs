using System.Collections.Generic;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Extension functionality for <see cref="IAssembly"/>.
/// </summary>
internal static class AssemblyExtensions
{
    /// <summary>
    /// Looks up the module in an assembly based on the module symbol.
    /// </summary>
    /// <param name="assembly">The assembly to lookup in.</param>
    /// <param name="module">The module to look up.</param>
    /// <returns>The looked up module within <paramref name="assembly"/>.</returns>
    public static IModule Lookup(this IAssembly assembly, ModuleSymbol module)
    {
        if (ReferenceEquals(assembly.RootModule.Symbol, module)) return assembly.RootModule;
        if (module.ContainingSymbol is not ModuleSymbol containingModule)
        {
            throw new KeyNotFoundException("the module could not be resolved based on the symbol");
        }
        var parentModule = Lookup(assembly, containingModule);
        return parentModule.Submodules[module];
    }
}
