using System.Collections.Immutable;
using System.Reflection.Metadata;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Helper for decoding signature types.
/// </summary>
internal sealed class SignatureDecoder : ISignatureTypeProvider<TypeSymbol, Unit>
{
    public static SignatureDecoder Instance { get; } = new();

    private SignatureDecoder()
    {
    }

    public TypeSymbol GetArrayType(TypeSymbol elementType, ArrayShape shape) => throw new UnsupportedMetadataException();
    public TypeSymbol GetByReferenceType(TypeSymbol elementType) => throw new UnsupportedMetadataException();
    public TypeSymbol GetFunctionPointerType(MethodSignature<TypeSymbol> signature) => throw new UnsupportedMetadataException();
    public TypeSymbol GetGenericInstantiation(TypeSymbol genericType, ImmutableArray<TypeSymbol> typeArguments) => throw new UnsupportedMetadataException();
    public TypeSymbol GetGenericMethodParameter(Unit genericContext, int index) => throw new UnsupportedMetadataException();
    public TypeSymbol GetGenericTypeParameter(Unit genericContext, int index) => throw new UnsupportedMetadataException();
    public TypeSymbol GetModifiedType(TypeSymbol modifier, TypeSymbol unmodifiedType, bool isRequired) => throw new UnsupportedMetadataException();
    public TypeSymbol GetPinnedType(TypeSymbol elementType) => throw new UnsupportedMetadataException();
    public TypeSymbol GetPointerType(TypeSymbol elementType) => throw new UnsupportedMetadataException();
    public TypeSymbol GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
    {
        PrimitiveTypeCode.Boolean => IntrinsicSymbols.Bool,
        PrimitiveTypeCode.Int32 => IntrinsicSymbols.Int32,
        PrimitiveTypeCode.String => IntrinsicSymbols.String,
        PrimitiveTypeCode.Void => IntrinsicSymbols.Unit,
        _ => throw new UnsupportedMetadataException(),
    };
    public TypeSymbol GetSZArrayType(TypeSymbol elementType) => throw new UnsupportedMetadataException();
    public TypeSymbol GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => throw new UnsupportedMetadataException();
    public TypeSymbol GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => throw new UnsupportedMetadataException();
    public TypeSymbol GetTypeFromSpecification(MetadataReader reader, Unit genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => throw new UnsupportedMetadataException();
}
