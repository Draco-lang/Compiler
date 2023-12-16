using System.Collections.Immutable;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A built-in primitive.
/// </summary>
internal sealed class PrimitiveTypeSymbol : TypeSymbol
{
    public override string Name { get; }
    public override bool IsValueType { get; }

    public PrimitiveTypeSymbol(string name, bool isValueType)
    {
        this.Name = name;
        this.IsValueType = isValueType;
    }

    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        base.GenericInstantiate(containingSymbol, arguments);
    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) => this;

    public override string ToString() => this.Name;
}
