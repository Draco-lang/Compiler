using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents an unary operator.
/// </summary>
internal abstract partial class UnaryOperatorSymbol : FunctionSymbol
{
    /// <summary>
    /// The operand of the operator.
    /// </summary>
    public abstract ParameterSymbol Operand { get; }
}
