using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Alias for a type. Not a real type itself.
/// </summary>
internal abstract class TypeAliasSymbol : Symbol
{
    /// <summary>
    /// The type being aliased.
    /// </summary>
    public abstract TypeSymbol Substitution { get; }
}
