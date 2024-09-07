using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Extension functionality for the IR model.
/// </summary>
internal static class ModelExtensions
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

    /// <summary>
    /// Retrieves all functions that are statically referenced by a given procedure.
    /// </summary>
    /// <param name="procedure">The procedure to retrieve the referenced functions from.</param>
    /// <returns>The referenced functions.</returns>
    public static IEnumerable<FunctionSymbol> GetReferencedFunctions(this IProcedure procedure) => procedure.BasicBlocks.Values
        .SelectMany(bb => bb.Instructions)
        .SelectMany(instr => instr.StaticOperands)
        .OfType<FunctionSymbol>()
        .Select(f => f.IsGenericInstance ? f.GenericDefinition! : f)
        .Distinct();
}
