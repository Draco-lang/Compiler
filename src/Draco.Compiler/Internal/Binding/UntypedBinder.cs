using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Constructs the untyped tree from the syntax tree.
/// </summary>
internal abstract partial class UntypedBinder
{
    /// <summary>
    /// A filter delegate for symbols.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>True, if the filter accepts <paramref name="symbol"/>.</returns>
    protected delegate bool SymbolFilter(Symbol symbol);

    /// <summary>
    /// The binder that gets invoked, if something could not be resolved in this one.
    /// In other terms, this is the parent scope.
    /// </summary>
    protected UntypedBinder? Parent { get; }

    /// <summary>
    /// Attempts to look up a symbol in this scope.
    /// </summary>
    /// <param name="result">The result to write the found symbols to.</param>
    /// <param name="name">The name of the symbol.</param>
    /// <param name="filter">The filter to use for the symbols.</param>
    protected virtual void LookupLocal(LookupResult result, string name, SymbolFilter filter)
    {
    }
}
