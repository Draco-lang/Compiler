using System.Collections.Generic;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Read-only interface of a procedure.
/// </summary>
internal interface IProcedure : IOperand
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
    /// The assembly this procedure is defined in.
    /// </summary>
    public IAssembly Assembly { get; }

    /// <summary>
    /// The entry basic block of this procedure.
    /// </summary>
    public IBasicBlock Entry { get; }

    /// <summary>
    /// All parameters for this procedure.
    /// </summary>
    public IReadOnlyDictionary<ParameterSymbol, Parameter> Parameters { get; }

    /// <summary>
    /// The parameters in the order they were defined.
    /// </summary>
    public IEnumerable<Parameter> ParametersInDefinitionOrder { get; }

    /// <summary>
    /// The return type of this procedure.
    /// </summary>
    public Type ReturnType { get; }

    /// <summary>
    /// All basic blocks within this procedure.
    /// </summary>
    public IReadOnlyDictionary<LabelSymbol, IBasicBlock> BasicBlocks { get; }

    /// <summary>
    /// The basic blocks in the order they were defined.
    /// </summary>
    public IEnumerable<IBasicBlock> BasicBlocksInDefinitionOrder { get; }

    /// <summary>
    /// The locals defined within this procedure.
    /// </summary>
    public IReadOnlyDictionary<LocalSymbol, Local> Locals { get; }

    /// <summary>
    /// The locals in the order they were defined.
    /// </summary>
    public IEnumerable<Local> LocalsInDefinitionOrder { get; }

    /// <summary>
    /// The registers in this procedure.
    /// </summary>
    public IReadOnlyList<Register> Registers { get; }
}
