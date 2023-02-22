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
    /// True, if this result is collecting function overloads.
    /// </summary>
    public bool IsOverloadSet =>
           this.Symbols.Count > 0
        && this.Symbols[0] is FunctionSymbol;

    /// <summary>
    /// The symbols found during lookup.
    /// </summary>
    public IReadOnlyList<Symbol> Symbols => this.symbols;
    private readonly List<Symbol> symbols = new();

    /// <summary>
    /// Attempts to add a symbol to the result set.
    /// </summary>
    /// <param name="symbol">The symbol to add.</param>
    /// <returns>True, if <paramref name="symbol"/> fits into the set and can be added.</returns>
    public bool Add(Symbol symbol)
    {
        if (this.IsOverloadSet)
        {
            // Only add functions
            if (symbol is not FunctionSymbol) return false;

            this.symbols.Add(symbol);
            return true;
        }
        else if (this.FoundAny)
        {
            // There are symbols already, don't add a function symbol, can't be an overload set anymore
            if (symbol is FunctionSymbol) return false;

            this.symbols.Add(symbol);
            return true;
        }
        else
        {
            // Can be anything
            this.symbols.Add(symbol);
            return true;
        }
    }
}
