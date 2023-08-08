using System.Collections.Immutable;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A built-in primitive.
/// </summary>
internal class PrimitiveTypeSymbol : TypeSymbol
{
    public override Symbol? ContainingSymbol => null;
    public override string Name { get; }
    public override string MetadataFullName { get; }
    public override string MetadataName => this.MetadataFullName.Split('.')[^1];
    public override bool IsValueType { get; }

    public PrimitiveTypeSymbol(string name, bool isValueType)
    {
        this.Name = name;
        this.IsValueType = isValueType;
        this.MetadataFullName = name;
    }

    public PrimitiveTypeSymbol(string name, string metadataFullName, bool isValueType)
    {
        this.Name = name;
        this.IsValueType = isValueType;
        this.MetadataFullName = metadataFullName;
    }

    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        base.GenericInstantiate(containingSymbol, arguments);
    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) => this;

    public override string ToString() => this.Name;
}
