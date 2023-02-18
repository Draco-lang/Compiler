using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Any variable-like symbol.
/// </summary>
internal abstract partial class VariableSymbol : Symbol
{
    /// <summary>
    /// The type of the local.
    /// </summary>
    public abstract Type Type { get; }

    /// <summary>
    /// True, if this local is mutable.
    /// </summary>
    public abstract bool IsMutable { get; }
}
