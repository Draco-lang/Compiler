using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// The result of a symbol lookup.
/// </summary>
internal sealed class LookupResult
{
    /// <summary>
    /// True, if symbols have been found during the lookup.
    /// </summary>
    public bool FoundAny => this.Symbols.Any();

    /// <summary>
    /// The symbols found during lookup.
    /// </summary>
    public IList<Symbol> Symbols { get; } = new List<Symbol>();
}
