using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Types;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Helper for decoding signature types.
/// </summary>
internal sealed class SignatureDecoder : ISignatureTypeProvider<Type, Unit>
{
    public static SignatureDecoder Instance { get; } = new();

    private SignatureDecoder()
    {
    }

    public Type GetArrayType(Type elementType, ArrayShape shape) => throw new UnsupportedMetadataException();
    public Type GetByReferenceType(Type elementType) => throw new UnsupportedMetadataException();
    public Type GetFunctionPointerType(MethodSignature<Type> signature) => throw new UnsupportedMetadataException();
    public Type GetGenericInstantiation(Type genericType, ImmutableArray<Type> typeArguments) => IntrinsicTypes.Error;
    public Type GetGenericMethodParameter(Unit genericContext, int index) => throw new UnsupportedMetadataException();
    public Type GetGenericTypeParameter(Unit genericContext, int index) => throw new UnsupportedMetadataException();
    public Type GetModifiedType(Type modifier, Type unmodifiedType, bool isRequired) => throw new UnsupportedMetadataException();
    public Type GetPinnedType(Type elementType) => throw new UnsupportedMetadataException();
    public Type GetPointerType(Type elementType) => throw new UnsupportedMetadataException();
    public Type GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
    {
        PrimitiveTypeCode.Boolean => IntrinsicTypes.Bool,
        PrimitiveTypeCode.Int32 => IntrinsicTypes.Int32,
        PrimitiveTypeCode.String => IntrinsicTypes.String,
        PrimitiveTypeCode.Void => IntrinsicTypes.Unit,
        _ => throw new UnsupportedMetadataException(),
    };
    public Type GetSZArrayType(Type elementType) => throw new UnsupportedMetadataException();
    public Type GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => throw new UnsupportedMetadataException();
    public Type GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => throw new UnsupportedMetadataException();
    public Type GetTypeFromSpecification(MetadataReader reader, Unit genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => throw new UnsupportedMetadataException();
}
