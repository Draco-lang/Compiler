using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Internal.OptimizingIr;
using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// An intrinsic that provides its IR codegen.
/// </summary>
internal abstract class IrFunctionSymbol : FunctionSymbol
{
    /// <summary>
    /// The codegen delegate.
    /// </summary>
    /// <param name="codegen">The code generator.</param>
    /// <param name="target">The register to store the result at.</param>
    /// <param name="operands">The compiled operand references.</param>
    public delegate void CodegenDelegate(
        FunctionBodyCodegen codegen,
        Register target,
        ImmutableArray<IOperand> operands);

    /// <summary>
    /// The codegen function.
    /// </summary>
    public abstract CodegenDelegate Codegen { get; }
}
