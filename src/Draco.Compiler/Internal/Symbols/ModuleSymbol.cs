using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a compilation unit.
/// </summary>
internal abstract partial class ModuleSymbol : Symbol
{
    /// <summary>
    /// The functions defined within this module.
    /// </summary>
    public abstract ImmutableArray<FunctionSymbol> Functions { get; }
}
