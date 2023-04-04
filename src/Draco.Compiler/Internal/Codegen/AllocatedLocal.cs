using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Some method-local variable allocation.
/// </summary>
/// <param name="Operand">The corresponding IR operand.</param>
/// <param name="Index">The index of the local within the method.</param>
internal readonly record struct AllocatedLocal(
    IOperand Operand,
    int Index);
