using System;
using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;
internal class AttributeDecoder<T> : ICustomAttributeTypeProvider<T>
{
    public T GetPrimitiveType(PrimitiveTypeCode typeCode) => throw new NotImplementedException();
    public T GetSystemType() => throw new NotImplementedException();
    public T GetSZArrayType(T elementType) => throw new NotImplementedException();
    public T GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => throw new NotImplementedException();
    public T GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => throw new NotImplementedException();
    public T GetTypeFromSerializedName(string name) => throw new NotImplementedException();
    public PrimitiveTypeCode GetUnderlyingEnumType(T type) => throw new NotImplementedException();
    public bool IsSystemType(T type) => throw new NotImplementedException();
}
