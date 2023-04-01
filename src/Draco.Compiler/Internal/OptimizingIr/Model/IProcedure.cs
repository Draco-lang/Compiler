using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Types;

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
    /// The return type of this procedure.
    /// </summary>
    public Type ReturnType { get; }

    /// <summary>
    /// All basic blocks within this procedure.
    /// </summary>
    public IReadOnlyDictionary<LabelSymbol, IBasicBlock> BasicBlocks { get; }

    /// <summary>
    /// The locals defined within this procedure.
    /// </summary>
    public IReadOnlyDictionary<LocalSymbol, Local> Locals { get; }
}
