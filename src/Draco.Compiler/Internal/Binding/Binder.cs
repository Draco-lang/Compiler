using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Represents a single scope that binds the syntax-tree to the untyped-tree and then the bound-tree.
/// </summary>
internal abstract partial class Binder
{
    /// <summary>
    /// The parent binder of this one.
    /// </summary>
    protected Binder? Parent { get; }

    /// <summary>
    /// Attempts to look up symbols in this binder only.
    /// </summary>
    /// <param name="result">The result to write the lookup results to.</param>
    /// <param name="name">The name of the symbols to search for.</param>
    protected virtual void LookupSymbolsLocally(LookupResult result, string name) { }
}
