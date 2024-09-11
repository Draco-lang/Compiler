using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Symbols.Synthetized.Array;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// The decoder directly reading metadata, but does not give any exact type for the symbols.
/// This is because some symbols (like static classes) decode as types in .NET world but as modules in Draco world.
/// </summary>
internal sealed class RawTypeProvider(Compilation compilation)
    : ISignatureTypeProvider<Symbol, Symbol>, ICustomAttributeTypeProvider<Symbol>
{
    // We have 2 levels of caching to avoid re-creating types
    // The first level is the "outer" level, which caches types by their metadata handle
    // The second level is the "inner" level, which caches types by their fully qualified name
    // Generally the first level is faster, but the second level is necessary for cross-assembly types and
    // different type reference encodings

    // Outer level cache key
    private readonly record struct LightCacheKey(MetadataReader Reader, EntityHandle Handle);

    // Inner level cache key
    private readonly record struct CacheKey(string AssemblyFullName, string TypeFullyQualifiedName);

    // TODO: We return a special error type for now to swallow errors
    private static TypeSymbol UnknownType { get; } = new PrimitiveTypeSymbol("<unknown>", false);

    private WellKnownTypes WellKnownTypes => compilation.WellKnownTypes;

    private readonly ConcurrentDictionary<LightCacheKey, TypeSymbol> lightCache = new();
    private readonly ConcurrentDictionary<CacheKey, TypeSymbol> cache = new();

    // Primitives, well-knowns /////////////////////////////////////////////////

    public bool IsSystemType(Symbol type) => ReferenceEquals(type, this.WellKnownTypes.SystemType);
    public Symbol GetSystemType() => this.WellKnownTypes.SystemType;

    public Symbol GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
    {
        PrimitiveTypeCode.Void => WellKnownTypes.Unit,

        PrimitiveTypeCode.Boolean => this.WellKnownTypes.SystemBoolean,
        PrimitiveTypeCode.Char => this.WellKnownTypes.SystemChar,

        PrimitiveTypeCode.SByte => this.WellKnownTypes.SystemSByte,
        PrimitiveTypeCode.Int16 => this.WellKnownTypes.SystemInt16,
        PrimitiveTypeCode.Int32 => this.WellKnownTypes.SystemInt32,
        PrimitiveTypeCode.Int64 => this.WellKnownTypes.SystemInt64,

        PrimitiveTypeCode.Byte => this.WellKnownTypes.SystemByte,
        PrimitiveTypeCode.UInt16 => this.WellKnownTypes.SystemUInt16,
        PrimitiveTypeCode.UInt32 => this.WellKnownTypes.SystemUInt32,
        PrimitiveTypeCode.UInt64 => this.WellKnownTypes.SystemUInt64,

        PrimitiveTypeCode.Single => this.WellKnownTypes.SystemSingle,
        PrimitiveTypeCode.Double => this.WellKnownTypes.SystemDouble,

        PrimitiveTypeCode.String => this.WellKnownTypes.SystemString,
        PrimitiveTypeCode.Object => this.WellKnownTypes.SystemObject,

        PrimitiveTypeCode.IntPtr => this.WellKnownTypes.SystemIntPtr,

        _ => UnknownType,
    };

    public PrimitiveTypeCode GetUnderlyingEnumType(Symbol symbol)
    {
        if (symbol is not TypeSymbol type) throw new ArgumentException("symbol is not a type", nameof(symbol));

        var fieldType = type.EnumUnderlyingType;
        if (fieldType is null) throw new NotSupportedException("no enum tag field found");

        if (SymbolEqualityComparer.Default.Equals(fieldType, this.WellKnownTypes.SystemByte)) return PrimitiveTypeCode.Byte;
        if (SymbolEqualityComparer.Default.Equals(fieldType, this.WellKnownTypes.SystemUInt16)) return PrimitiveTypeCode.UInt16;
        if (SymbolEqualityComparer.Default.Equals(fieldType, this.WellKnownTypes.SystemUInt32)) return PrimitiveTypeCode.UInt32;
        if (SymbolEqualityComparer.Default.Equals(fieldType, this.WellKnownTypes.SystemUInt64)) return PrimitiveTypeCode.UInt64;
        if (SymbolEqualityComparer.Default.Equals(fieldType, this.WellKnownTypes.SystemSByte)) return PrimitiveTypeCode.SByte;
        if (SymbolEqualityComparer.Default.Equals(fieldType, this.WellKnownTypes.SystemInt16)) return PrimitiveTypeCode.Int16;
        if (SymbolEqualityComparer.Default.Equals(fieldType, this.WellKnownTypes.SystemInt32)) return PrimitiveTypeCode.Int32;
        if (SymbolEqualityComparer.Default.Equals(fieldType, this.WellKnownTypes.SystemInt64)) return PrimitiveTypeCode.Int64;

        throw new NotSupportedException($"unsupported enum tag field type {fieldType}");
    }

    // TODO: These are not implemented /////////////////////////////////////////

    public Symbol GetPinnedType(Symbol elementType) => UnknownType;
    public Symbol GetPointerType(Symbol elementType) => UnknownType;
    public Symbol GetModifiedType(Symbol modifier, Symbol unmodifiedType, bool isRequired) => UnknownType;
    public Symbol GetTypeFromSerializedName(string name) => UnknownType;
    public Symbol GetFunctionPointerType(MethodSignature<Symbol> signature) => UnknownType;
    public Symbol GetByReferenceType(Symbol elementType) => UnknownType;

    // Trivial instantiations //////////////////////////////////////////////////

    public Symbol GetSZArrayType(Symbol elementType) =>
        this.WellKnownTypes.ArrayType.GenericInstantiate((TypeSymbol)elementType);
    public Symbol GetArrayType(Symbol elementType, ArrayShape shape) =>
        new ArrayTypeSymbol(compilation, shape.Rank, this.WellKnownTypes.SystemInt32).GenericInstantiate((TypeSymbol)elementType);

    // Generics ////////////////////////////////////////////////////////////////

    public Symbol GetGenericTypeParameter(Symbol genericContext, int index)
    {
        var typeAncestor = genericContext.AncestorChain
            // Both types and modules are a type in .NET world
            .Where(s => s is TypeSymbol or ModuleSymbol)
            .First();

        return typeAncestor.IsGenericDefinition
            ? typeAncestor.GenericParameters[index]
            : typeAncestor.GenericDefinition!.GenericParameters[index];
    }

    public Symbol GetGenericMethodParameter(Symbol genericContext, int index)
    {
        var methodAncestor = genericContext.AncestorChain
            .OfType<FunctionSymbol>()
            .First();

        return methodAncestor.IsGenericDefinition
            ? methodAncestor.GenericParameters[index]
            : methodAncestor.GenericDefinition!.GenericParameters[index];
    }

    public Symbol GetGenericInstantiation(Symbol genericType, ImmutableArray<Symbol> typeArguments)
    {
        if (ReferenceEquals(genericType, UnknownType)) return UnknownType;
        return genericType.GenericInstantiate(genericType.ContainingSymbol, typeArguments.Cast<TypeSymbol>().ToImmutableArray());
    }

    // Reading up from reference or definition /////////////////////////////////

    public Symbol GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => throw new System.NotImplementedException();
    public Symbol GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => throw new System.NotImplementedException();
    public Symbol GetTypeFromSpecification(MetadataReader reader, Symbol genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => throw new System.NotImplementedException();
}
