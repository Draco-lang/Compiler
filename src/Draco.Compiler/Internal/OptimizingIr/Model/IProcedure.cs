using System.Collections.Generic;
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
    /// The name of this procedure.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The module this procedure is defined in.
    /// </summary>
    public IModule DeclaringModule { get; }

    /// <summary>
    /// The assembly this procedure is defined in.
    /// </summary>
    public IAssembly Assembly { get; }

    /// <summary>
    /// The entry basic block of this procedure.
    /// </summary>
    public IBasicBlock Entry { get; }

    /// <summary>
    /// The generic parameters on this procedure.
    /// </summary>
    public IReadOnlyList<TypeParameterSymbol> Generics { get; }

    /// <summary>
    /// All parameters for this procedure in the order they were defined.
    /// </summary>
    public IReadOnlyList<ParameterSymbol> Parameters { get; }

    /// <summary>
    /// The return type of this procedure.
    /// </summary>
    public TypeSymbol ReturnType { get; }

    /// <summary>
    /// All basic blocks within this procedure.
    /// </summary>
    public IReadOnlyDictionary<LabelSymbol, IBasicBlock> BasicBlocks { get; }

    /// <summary>
    /// The basic blocks in the order they were defined.
    /// </summary>
    public IEnumerable<IBasicBlock> BasicBlocksInDefinitionOrder { get; }

    /// <summary>
    /// The locals defined within this procedure, in definition order.
    /// </summary>
    public IReadOnlyList<LocalSymbol> Locals { get; }

    /// <summary>
    /// The registers in this procedure.
    /// </summary>
    public IReadOnlyList<Register> Registers { get; }

    /// <summary>
    /// Retrieves the index of the given parameter.
    /// </summary>
    /// <param name="symbol">The parameter symbol to get the index of.</param>
    /// <returns>The (0-based) index of the parameter.</returns>
    public int GetParameterIndex(ParameterSymbol symbol);
}
