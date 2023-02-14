using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a binary operator.
/// </summary>
internal abstract partial class BinaryOperatorSymbol : FunctionSymbol
{
    /// <summary>
    /// The left operand of the operator.
    /// </summary>
    public abstract ParameterSymbol Left { get; }

    /// <summary>
    /// The right operand of the operator.
    /// </summary>
    public abstract ParameterSymbol Right { get; }
}
