using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Utility for building a collection of symbols to return from an API.
/// Automatically groups sets of overloaded symbols together.
/// </summary>
internal sealed class SymbolCollectionBuilder
{
    /// <summary>
    /// Converts a sequence of symbols to a collection sequence.
    /// </summary>
    /// <param name="symbols">The symbols to convert.</param>
    /// <param name="allowSpecialName">True, if special names are allowed.</param>
    /// <param name="visibleFrom">The symbol to check visibility from, if any.</param>
    /// <returns>The converted symbols.</returns>
    public static IEnumerable<Symbol> ToCollection(
        IEnumerable<Symbol> symbols,
        bool allowSpecialName = false,
        Symbol? visibleFrom = null)
    {
        var builder = new SymbolCollectionBuilder()
        {
            AllowSpecialName = allowSpecialName,
            VisibleFrom = visibleFrom,
        };
        foreach (var symbol in symbols) builder.Add(symbol);
        return builder.EnumerateResult();
    }

    /// <summary>
    /// True, if special names are allowed.
    /// </summary>
    public bool AllowSpecialName { get; init; }

    /// <summary>
    /// The symbol to check visibility from, if any.
    /// </summary>
    public Symbol? VisibleFrom { get; init; }

    private readonly HashSet<Symbol> nonFunctionSymbols = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<string, HashSet<FunctionSymbol>> functionSymbols = [];

    /// <summary>
    /// Builds the collection of symbols.
    /// </summary>
    /// <returns>The collection of symbols.</returns>
    public ImmutableArray<Symbol> Build() => [.. this.EnumerateResult()];

    /// <summary>
    /// Enumerates the result.
    /// </summary>
    /// <returns>The result enumerable.</returns>
    public IEnumerable<Symbol> EnumerateResult()
    {
        foreach (var symbol in this.nonFunctionSymbols) yield return symbol;
        foreach (var set in this.functionSymbols.Values)
        {
            if (set.Count == 1)
            {
                yield return set.First();
            }
            else
            {
                yield return new FunctionGroupSymbol([.. set]);
            }
        }
    }

    /// <summary>
    /// Adds a symbol to the collection.
    /// </summary>
    /// <param name="symbol">The symbol to add.</param>
    public void Add(Symbol symbol)
    {
        if (!this.AllowSpecialName && symbol.IsSpecialName) return;
        if (this.VisibleFrom is not null && !symbol.IsVisibleFrom(this.VisibleFrom)) return;

        if (symbol is FunctionSymbol functionSymbol)
        {
            this.GetFunctionSet(functionSymbol.Name).Add(functionSymbol);
        }
        else
        {
            this.nonFunctionSymbols.Add(symbol);
        }
    }

    /// <summary>
    /// Adds a range of symbols to the collection.
    /// </summary>
    /// <param name="symbols">The symbols to add.</param>
    public void AddRange(IEnumerable<Symbol> symbols)
    {
        foreach (var symbol in symbols) this.Add(symbol);
    }

    private HashSet<FunctionSymbol> GetFunctionSet(string name)
    {
        if (!this.functionSymbols.TryGetValue(name, out var set))
        {
            set = new(SymbolEqualityComparer.Default);
            this.functionSymbols.Add(name, set);
        }
        return set;
    }
}
