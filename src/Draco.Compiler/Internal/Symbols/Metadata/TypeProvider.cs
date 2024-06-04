using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Helper for decoding metadata blob-encoded types.
/// </summary>
internal sealed class TypeProvider : ISignatureTypeProvider<TypeSymbol, Symbol>, ICustomAttributeTypeProvider<TypeSymbol>
{
    private readonly record struct CacheKey(MetadataReader Reader, EntityHandle Handle);

    // TODO: We return a special error type for now to swallow errors
    private static TypeSymbol UnknownType { get; } = new PrimitiveTypeSymbol("<unknown>", false);

    private WellKnownTypes WellKnownTypes => this.compilation.WellKnownTypes;

    private readonly Compilation compilation;
    private readonly ConcurrentDictionary<CacheKey, TypeSymbol> cache = new();

    public TypeProvider(Compilation compilation)
    {
        this.compilation = compilation;
    }

    public TypeSymbol GetArrayType(TypeSymbol elementType, ArrayShape shape) =>
        new ArrayTypeSymbol(shape.Rank, this.WellKnownTypes.SystemInt32).GenericInstantiate(elementType);
    public TypeSymbol GetSZArrayType(TypeSymbol elementType) =>
        this.WellKnownTypes.ArrayType.GenericInstantiate(elementType);
    public TypeSymbol GetByReferenceType(TypeSymbol elementType) => UnknownType;
    public TypeSymbol GetFunctionPointerType(MethodSignature<TypeSymbol> signature) => UnknownType;
    public TypeSymbol GetGenericInstantiation(TypeSymbol genericType, ImmutableArray<TypeSymbol> typeArguments)
    {
        if (ReferenceEquals(genericType, UnknownType)) return UnknownType;
        return genericType.GenericInstantiate(genericType.ContainingSymbol, typeArguments);
    }

    public TypeSymbol GetGenericMethodParameter(Symbol genericContext, int index)
    {
        var methodAncestor = genericContext.AncestorChain
            .OfType<FunctionSymbol>()
            .First();

        return methodAncestor.IsGenericDefinition
            ? methodAncestor.GenericParameters[index]
            : methodAncestor.GenericDefinition!.GenericParameters[index];
    }

    public TypeSymbol GetGenericTypeParameter(Symbol genericContext, int index)
    {
        var typeAncestor = genericContext.AncestorChain
            .OfType<TypeSymbol>()
            .First();

        return typeAncestor.IsGenericDefinition
            ? typeAncestor.GenericParameters[index]
            : typeAncestor.GenericDefinition!.GenericParameters[index];
    }

    public TypeSymbol GetModifiedType(TypeSymbol modifier, TypeSymbol unmodifiedType, bool isRequired) => UnknownType;
    public TypeSymbol GetPinnedType(TypeSymbol elementType) => UnknownType;
    public TypeSymbol GetPointerType(TypeSymbol elementType) => UnknownType;
    public TypeSymbol GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
    {
        PrimitiveTypeCode.Void => WellKnownTypes.Unit,

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

        PrimitiveTypeCode.Boolean => this.WellKnownTypes.SystemBoolean,
        PrimitiveTypeCode.Char => this.WellKnownTypes.SystemChar,

        PrimitiveTypeCode.String => this.WellKnownTypes.SystemString,
        PrimitiveTypeCode.Object => this.WellKnownTypes.SystemObject,

        _ => UnknownType,
    };

    public TypeSymbol GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        var key = new CacheKey(reader, handle);
        return this.cache.GetOrAdd(key, _ => this.BuildTypeFromDefinition(reader, handle, rawTypeKind));
    }

    private TypeSymbol BuildTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        var definition = reader.GetTypeDefinition(handle);
        if (definition.IsNested)
        {
            // Resolve declaring type
            var declaringType = definition.GetDeclaringType();
            var declaringSymbol = this.GetTypeFromDefinition(reader, declaringType, rawTypeKind);

            // Search for this type by name and generic argument count
            var nestedName = reader.GetString(definition.Name);
            var nestedGenericArgc = definition.GetGenericParameters().Count;
            return declaringSymbol
                .DefinedMembers
                .OfType<TypeSymbol>()
                .Where(t => t.Name == nestedName && t.GenericParameters.Length == nestedGenericArgc)
                .Single();
        }

        var assemblyName = reader
            .GetAssemblyDefinition()
            .GetAssemblyName();

        // Type path
        var @namespace = reader.GetString(definition.Namespace);
        var name = reader.GetString(definition.Name);
        var fullName = string.IsNullOrWhiteSpace(@namespace) ? name : $"{@namespace}.{name}";
        var path = fullName.Split('.').ToImmutableArray();

        return this.WellKnownTypes.GetTypeFromAssembly(assemblyName, path);
    }

    public TypeSymbol GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var key = new CacheKey(reader, handle);
        return this.cache.GetOrAdd(key, _ => this.BuildTypeFromReference(reader, handle, rawTypeKind));
    }

    private TypeSymbol BuildTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var parts = new List<string>();
        var reference = reader.GetTypeReference(handle);
        parts.Add(reader.GetString(reference.Name));
        EntityHandle resolutionScope;
        for (resolutionScope = reference.ResolutionScope; resolutionScope.Kind == HandleKind.TypeReference; resolutionScope = reference.ResolutionScope)
        {
            reference = reader.GetTypeReference((TypeReferenceHandle)resolutionScope);
            parts.Add(reader.GetString(reference.Name));
        }
        var @namespace = reader.GetString(reference.Namespace);
        if (!string.IsNullOrEmpty(@namespace)) parts.AddRange(@namespace.Split('.').Reverse());
        parts.Reverse();

        // TODO: If we don't have the assembly report error
        var assemblyName = reader.GetAssemblyReference((AssemblyReferenceHandle)resolutionScope).GetAssemblyName();
        var assembly = this.compilation.MetadataAssemblies.Values.Single(x => x.AssemblyName.FullName == assemblyName.FullName);
        return assembly.RootNamespace.Lookup(parts.ToImmutableArray()).OfType<TypeSymbol>().Single();
    }

    // TODO: Should we cache this as well? doesn't seem to have any effect
    public TypeSymbol GetTypeFromSpecification(MetadataReader reader, Symbol genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
    {
        var specification = reader.GetTypeSpecification(handle);
        return specification.DecodeSignature(this, genericContext);
    }

    public TypeSymbol GetSystemType() => this.WellKnownTypes.SystemType;
    public bool IsSystemType(TypeSymbol type) => ReferenceEquals(type, this.WellKnownTypes.SystemType);
    public TypeSymbol GetTypeFromSerializedName(string name) => UnknownType;
    public PrimitiveTypeCode GetUnderlyingEnumType(TypeSymbol type) => throw new System.ArgumentOutOfRangeException(nameof(type));
}
