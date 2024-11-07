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
