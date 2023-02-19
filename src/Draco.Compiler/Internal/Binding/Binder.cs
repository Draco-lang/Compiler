using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Represents a single scope that binds the syntax-tree to the untyped-tree and then the bound-tree.
/// </summary>
internal abstract partial class Binder
{
    /// <summary>
    /// A predicate for filtering symbols.
    /// </summary>
    /// <param name="symbol">The symbol to be considered.</param>
    /// <returns>True, if the symbol should be considered in the filtering.</returns>
    protected delegate bool SymbolFilter(Symbol symbol);

    /// <summary>
    /// The compilation this binder was created for.
    /// </summary>
    protected Compilation Compilation { get; }

    /// <summary>
    /// The parent binder of this one.
    /// </summary>
    protected Binder? Parent { get; }

    /// <summary>
    /// The symbols defined in the scope of this binder.
    /// </summary>
    protected virtual IEnumerable<Symbol> Symbols => Enumerable.Empty<Symbol>();

    protected Binder(Compilation compilation)
    {
        this.Compilation = compilation;
    }

    protected Binder(Binder parent)
        : this(parent.Compilation)
    {
        this.Parent = parent;
    }

    /// <summary>
    /// Attempts to look up symbols in this binder only.
    /// </summary>
    /// <param name="result">The result to write the lookup results to.</param>
    /// <param name="name">The name of the symbols to search for.</param>
    /// <param name="filter">The filter to use.</param>
    protected void LookupSymbolsLocally(LookupResult result, string name, SymbolFilter filter)
    {
        foreach (var member in this.Symbols)
        {
            if (member.Name != name) continue;
            if (!filter(member)) continue;
            result.Symbols.Add(member);
        }
    }
}
