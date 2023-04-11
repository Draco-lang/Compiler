using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Draco.Compiler.Api;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols;

internal sealed class ArrayTypeSymbol : TypeSymbol
{
    private readonly TypeSymbol systemArrayType;

    public ArrayTypeSymbol(TypeSymbol elementType, int rank, TypeSymbol systemArrayType)
    {
        this.ElementType = elementType;
        this.Rank = rank;
        this.systemArrayType = systemArrayType;
    }

    public TypeSymbol ElementType { get; }

    public int Rank { get; }

    public override Symbol? ContainingSymbol => null;

    public override string ToString() => $"[]{this.ElementType}";

    private ImmutableArray<SynthetizedArrayFunctionSymbol>? arrayMembers;

    private ImmutableArray<SynthetizedArrayFunctionSymbol> BuildArrayMembers() => ImmutableArray.Create
        (
            new SynthetizedArrayFunctionSymbol(this, SynthetizedArrayFunctionSymbol.FunctionKind.Constructor),
            new(this, SynthetizedArrayFunctionSymbol.FunctionKind.Get),
            new(this, SynthetizedArrayFunctionSymbol.FunctionKind.Set)
        );

    public FunctionSymbol ConstructorFunction => (this.arrayMembers ??= this.BuildArrayMembers())[0];
    public FunctionSymbol GetFunction => (this.arrayMembers ??= this.BuildArrayMembers())[1];
    public FunctionSymbol SetFunction => (this.arrayMembers ??= this.BuildArrayMembers())[2];


    public override IEnumerable<Symbol> Members => this.systemArrayType.Members.Concat(this.arrayMembers ??= this.BuildArrayMembers());

    public override Compilation? DeclaringCompilation => this.systemArrayType.DeclaringCompilation;
}
