using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Draco.Compiler.Internal.Symbols.Generic;

/// <summary>
/// Represents a generic context (substituted type-variables).
/// </summary>
internal readonly struct GenericContext : IReadOnlyDictionary<TypeParameterSymbol, TypeSymbol>
{
    public int Count => this.substitutions.Count;

    public IEnumerable<TypeParameterSymbol> Keys => this.substitutions.Keys;
    public IEnumerable<TypeSymbol> Values => this.substitutions.Values;

    public TypeSymbol this[TypeParameterSymbol key] => this.substitutions[key];

    private readonly ImmutableDictionary<TypeParameterSymbol, TypeSymbol> substitutions;

    public GenericContext(ImmutableDictionary<TypeParameterSymbol, TypeSymbol> substitutions)
    {
        this.substitutions = substitutions;
    }

    public GenericContext(IEnumerable<KeyValuePair<TypeParameterSymbol, TypeSymbol>> substitutions)
        : this(substitutions.ToImmutableDictionary())
    {
    }

    public GenericContext Merge(GenericContext other)
    {
        var substitutions = ImmutableDictionary.CreateBuilder<TypeParameterSymbol, TypeSymbol>();
        substitutions.AddRange(this);
        // Go through existing substitutions and where we have X -> Y in the old, Y -> Z in the new,
        // replace with X -> Z
        foreach (var (typeParam, typeSubst) in this)
        {
            if (typeSubst is not TypeParameterSymbol paramSubst) continue;
            if (other.TryGetValue(paramSubst, out var prunedSubst))
            {
                substitutions[typeParam] = prunedSubst;
            }
        }
        // Add the rest
        foreach (var (typeParam, type) in other) substitutions[typeParam] = type;
        // Done merging
        return new GenericContext(substitutions.ToImmutable());
    }

    public bool TryGetValue(TypeParameterSymbol key, [MaybeNullWhen(false)] out TypeSymbol value) =>
        this.substitutions.TryGetValue(key, out value);
    public bool ContainsKey(TypeParameterSymbol key) => this.substitutions.ContainsKey(key);
    public IEnumerator<KeyValuePair<TypeParameterSymbol, TypeSymbol>> GetEnumerator() => this.substitutions.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
