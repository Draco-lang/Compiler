using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Api;

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

    public override Compilation DeclaringCompilation { get; }

    public override ImmutableArray<TypeSymbol> ImmediateBaseTypes =>
        InterlockedUtils.InitializeDefault(ref this.immediateBaseTypes, this.BuildImmediateBaseTypes);
    private ImmutableArray<TypeSymbol> immediateBaseTypes;

    public override string Name => this.Rank switch
    {
        1 => "Array",
        int n => $"Array{n}D",
    };

    public override ImmutableArray<TypeParameterSymbol> GenericParameters => [this.ElementType];

    public override IEnumerable<Symbol> DefinedMembers =>
    [
        new ArrayIndexPropertySymbol(this),
    ];

    public ArrayTypeSymbol(Compilation compilation, int rank, TypeSymbol indexType)
    {
        this.DeclaringCompilation = compilation;
        this.ElementType = new SynthetizedTypeParameterSymbol(this, "T");
        this.Rank = rank;
        this.IndexType = indexType;
    }

    public TypeSymbol GenericInstantiate(TypeSymbol elementType) =>
        this.GenericInstantiate(containingSymbol: null, ImmutableArray.Create(elementType));

    public override string ToString() => $"{this.Name}{this.GenericsToString()}";

    private ImmutableArray<TypeSymbol> BuildImmediateBaseTypes()
    {
        var wellKnownTypes = this.DeclaringCompilation.WellKnownTypes;

        // We need to implement System.Array, IList, IReadOnlyList
        return [
            wellKnownTypes.SystemArray,
            wellKnownTypes.SystemCollectionsGenericIList_1.GenericInstantiate(this.ContainingSymbol, [this.ElementType]),
            wellKnownTypes.SystemCollectionsGenericIReadOnlyList_1.GenericInstantiate(this.ContainingSymbol, [this.ElementType])];
    }
}
