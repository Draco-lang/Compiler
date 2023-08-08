using System.Collections.Generic;
using System.Collections.Immutable;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// An array modeled as generics.
/// </summary>
internal sealed class ArrayTypeSymbol : TypeSymbol
{
    /// <summary>
    /// The element type this array stores.
    /// </summary>
    public TypeParameterSymbol ElementType { get; }

    /// <summary>
    /// The index type this array can be indexed with.
    /// </summary>
    public TypeSymbol IndexType { get; }

    /// <summary>
    /// The rank of the array (number of dimensions).
    /// </summary>
    public int Rank { get; }

    public override Symbol? ContainingSymbol => null;
    public override string Name => this.Rank switch
    {
        1 => "Array",
        int n => $"Array{n}D",
    };

    public override ImmutableArray<TypeParameterSymbol> GenericParameters => ImmutableArray.Create(this.ElementType);

    public override IEnumerable<Symbol> DefinedMembers => new[]
    {
        new ArrayLengthPropertySymbol(this) as Symbol,
        new ArrayIndexPropertySymbol(this),
    };

    public ArrayTypeSymbol(int rank, TypeSymbol indexType)
    {
        this.ElementType = new SynthetizedTypeParameterSymbol(this, "T");
        this.Rank = rank;
        this.IndexType = indexType;
    }

    public TypeSymbol GenericInstantiate(TypeSymbol elementType) =>
        this.GenericInstantiate(containingSymbol: null, ImmutableArray.Create(elementType));

    public override string ToString() => $"{this.Name}{this.GenericsToString()}";
}
