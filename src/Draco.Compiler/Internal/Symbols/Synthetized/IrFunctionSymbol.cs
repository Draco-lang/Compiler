using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.OptimizingIr;
using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// An <see cref="IntrinsicFunctionSymbol"/> that provides its IR codegen.
/// </summary>
internal sealed class IrFunctionSymbol : IntrinsicFunctionSymbol
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
    public CodegenDelegate Codegen { get; }

    public IrFunctionSymbol(
        string name,
        IEnumerable<TypeSymbol> paramTypes,
        TypeSymbol returnType,
        CodegenDelegate codegen)
        : base(name, paramTypes, returnType)
    {
        this.Codegen = codegen;
    }
}
