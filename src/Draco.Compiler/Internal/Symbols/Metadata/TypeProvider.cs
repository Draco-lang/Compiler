using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Helper for decoding metadata blob-encoded types.
/// </summary>
internal sealed class TypeProvider(RawTypeProvider underlying)
    : ISignatureTypeProvider<TypeSymbol, Symbol>, ICustomAttributeTypeProvider<TypeSymbol>
{
    // Queries for primitives
    public bool IsSystemType(TypeSymbol type) => underlying.IsSystemType(type);
    public TypeSymbol GetSystemType() => (TypeSymbol)underlying.GetSystemType();
    public TypeSymbol GetPrimitiveType(PrimitiveTypeCode typeCode) => (TypeSymbol)underlying.GetPrimitiveType(typeCode);
    public PrimitiveTypeCode GetUnderlyingEnumType(TypeSymbol type) => underlying.GetUnderlyingEnumType(type);

    // Type construction
    public TypeSymbol GetSZArrayType(TypeSymbol elementType) => (TypeSymbol)underlying.GetSZArrayType(elementType);
    public TypeSymbol GetArrayType(TypeSymbol elementType, ArrayShape shape) => (TypeSymbol)underlying.GetArrayType(elementType, shape);
    public TypeSymbol GetPinnedType(TypeSymbol elementType) => (TypeSymbol)underlying.GetPinnedType(elementType);
    public TypeSymbol GetPointerType(TypeSymbol elementType) => (TypeSymbol)underlying.GetPointerType(elementType);
    public TypeSymbol GetModifiedType(TypeSymbol modifier, TypeSymbol unmodifiedType, bool isRequired) =>
        (TypeSymbol)underlying.GetModifiedType(modifier, unmodifiedType, isRequired);
    public TypeSymbol GetFunctionPointerType(MethodSignature<TypeSymbol> signature) =>
        (TypeSymbol)underlying.GetFunctionPointerType(ToSymbolMethodSignature(signature));
    public TypeSymbol GetByReferenceType(TypeSymbol elementType) => (TypeSymbol)underlying.GetByReferenceType(elementType);

    // Generics
    public TypeSymbol GetGenericTypeParameter(Symbol genericContext, int index) => (TypeSymbol)underlying.GetGenericTypeParameter(genericContext, index);
    public TypeSymbol GetGenericMethodParameter(Symbol genericContext, int index) => (TypeSymbol)underlying.GetGenericMethodParameter(genericContext, index);
    public TypeSymbol GetGenericInstantiation(TypeSymbol genericType, ImmutableArray<TypeSymbol> typeArguments) =>
        (TypeSymbol)underlying.GetGenericInstantiation(genericType, typeArguments.Cast<Symbol>().ToImmutableArray());

    // Readup
    public TypeSymbol GetTypeFromSerializedName(string name) => (TypeSymbol)underlying.GetTypeFromSerializedName(name);
    public TypeSymbol GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
        (TypeSymbol)underlying.GetTypeFromDefinition(reader, handle, rawTypeKind);
    public TypeSymbol GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
        (TypeSymbol)underlying.GetTypeFromReference(reader, handle, rawTypeKind);
    public TypeSymbol GetTypeFromSpecification(MetadataReader reader, Symbol genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
        (TypeSymbol)underlying.GetTypeFromSpecification(reader, genericContext, handle, rawTypeKind);

    // Conversion

    private static MethodSignature<Symbol> ToSymbolMethodSignature(MethodSignature<TypeSymbol> signature) => new(
        header: signature.Header,
        returnType: signature.ReturnType,
        requiredParameterCount: signature.RequiredParameterCount,
        genericParameterCount: signature.GenericParameterCount,
        parameterTypes: signature.ParameterTypes.Cast<Symbol>().ToImmutableArray());
}
