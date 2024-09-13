using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Utility for building a collection of symbols to return from an API.
/// Automatically groups sets of overloaded symbols together.
/// </summary>
internal sealed class SymbolCollectionBuilder
{
    private readonly HashSet<Symbol> nonFunctionSymbols = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<string, HashSet<FunctionSymbol>> functionSymbols = [];

    /// <summary>
    /// Builds the collection of symbols.
    /// </summary>
    /// <returns>The collection of symbols.</returns>
    public ImmutableArray<Symbol> Build()
    {
        var builder = ImmutableArray.CreateBuilder<Symbol>(this.nonFunctionSymbols.Count + this.functionSymbols.Count);
        builder.AddRange(this.nonFunctionSymbols);
        foreach (var set in this.functionSymbols.Values)
        {
            if (set.Count == 1)
            {
                // Singletons don't need to be grouped
                builder.Add(set.First());
            }
            else
            {
                // Wrap the set
                builder.Add(new FunctionGroupSymbol(set.ToImmutableArray()));
            }
        }
        return builder.ToImmutable();
    }

    /// <summary>
    /// Adds a symbol to the collection.
    /// </summary>
    /// <param name="symbol">The symbol to add.</param>
    public void Add(Symbol symbol)
    {
        if (symbol is FunctionSymbol functionSymbol)
        {
            this.GetFunctionSet(functionSymbol.Name).Add(functionSymbol);
        }
        else
        {
            this.nonFunctionSymbols.Add(symbol);
        }
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
