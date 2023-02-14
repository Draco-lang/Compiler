using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Base for all symbols within the language.
/// </summary>
internal abstract partial class Symbol
{
    /// <summary>
    /// The symbol directly containing this one.
    /// </summary>
    public abstract Symbol? ContainingSymbol { get; }

    /// <summary>
    /// The name of this symbol.
    /// </summary>
    public virtual string Name => string.Empty;
}
