using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// A global variable.
/// </summary>
internal abstract partial class GlobalSymbol : Symbol
{
    /// <summary>
    /// The type of the global.
    /// </summary>
    public abstract Type Type { get; }
}
