using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Helper for decoding metadata blob-encoded types.
/// </summary>
internal sealed class TypeProvider(Compilation compilation)
    : ISignatureTypeProvider<TypeSymbol, Symbol>, ICustomAttributeTypeProvider<TypeSymbol>
{
    // We have 2 levels of caching to avoid re-creating types
    // The first level is the "outer" level, which caches types by their metadata handle
    // The second level is the "inner" level, which caches types by their fully qualified name
    // Generally the first level is faster, but the second level is necessary for cross-assembly types and
    // different type reference encodings

    private readonly record struct LightCacheKey(
        MetadataReader Reader,
        EntityHandle Handle);

    private readonly record struct CacheKey(
        string AssemblyFullName,
        string TypeFullyQualifiedName);

    // TODO: We return a special error type for now to swallow errors
    private static TypeSymbol UnknownType { get; } = new PrimitiveTypeSymbol("<unknown>", false);

    private WellKnownTypes WellKnownTypes => compilation.WellKnownTypes;

    private readonly ConcurrentDictionary<LightCacheKey, TypeSymbol> lightCache = new();
    private readonly ConcurrentDictionary<CacheKey, TypeSymbol> cache = new();

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

        PrimitiveTypeCode.IntPtr => this.WellKnownTypes.SystemIntPtr,

        _ => UnknownType,
    };

    public TypeSymbol GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        var lightKey = new LightCacheKey(reader, handle);
        return this.lightCache.GetOrAdd(lightKey, _ =>
        {
            // Check, if the type is already cached in the primary cache
            // For that we need to resolve the assembly name and the fully qualified name
            var assemblyName = reader.GetAssemblyDefinition().GetAssemblyName();

            var definition = reader.GetTypeDefinition(handle);
            var @namespace = reader.GetString(definition.Namespace);
            var name = reader.GetString(definition.Name);
            var fullName = ConcatenateNamespaceAndName(@namespace, name);

            var key = new CacheKey(assemblyName.FullName, fullName);
            return this.cache.GetOrAdd(key, _ => this.BuildTypeFromDefinition(reader, handle, rawTypeKind));
        });
    }

    public TypeSymbol GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var lightKey = new LightCacheKey(reader, handle);
        return this.lightCache.GetOrAdd(lightKey, _ =>
        {
            var key = BuildCacheKey(reader, handle);
            return this.cache.GetOrAdd(key, _ => this.BuildTypeFromReference(reader, handle, rawTypeKind));
        });
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
        var fullName = ConcatenateNamespaceAndName(@namespace, name);
        var path = fullName.Split('.').ToImmutableArray();

        return this.WellKnownTypes.GetTypeFromAssembly(assemblyName, path);
    }

    private TypeSymbol BuildTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var parts = new List<string>();
        var reference = reader.GetTypeReference(handle);
        var referenceName = reader.GetString(reference.Name);
        parts.Add(referenceName);
        EntityHandle resolutionScope;
        for (resolutionScope = reference.ResolutionScope; resolutionScope.Kind == HandleKind.TypeReference; resolutionScope = reference.ResolutionScope)
        {
            reference = reader.GetTypeReference((TypeReferenceHandle)resolutionScope);
            parts.Add(reader.GetString(reference.Name));
        }
        var @namespace = reader.GetString(reference.Namespace);
        if (!string.IsNullOrEmpty(@namespace)) parts.AddRange(@namespace.Split('.').Reverse());
        parts.Reverse();

        var assemblyName = reader.GetAssemblyReference((AssemblyReferenceHandle)resolutionScope).GetAssemblyName();
        var assembly = compilation.MetadataAssemblies.FirstOrDefault(x => AssemblyNamesEqual(x.AssemblyName, assemblyName));
        if (assembly is null)
        {
            // The assembly for some reason isn't included, report it
            compilation.GlobalDiagnosticBag.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.CanNotResolveReferencedAssembly,
                location: Location.None,
                formatArgs: assemblyName));
            return WellKnownTypes.ErrorType;
        }

        return assembly.RootNamespace.Lookup([.. parts]).OfType<TypeSymbol>().Single();
    }

    private static CacheKey BuildCacheKey(MetadataReader reader, TypeReferenceHandle handle)
    {
        // Directly the type itself
        var typeReference = reader.GetTypeReference(handle);
        var typeName = reader.GetString(typeReference.Name);
        // The reference might be nested, so we need to walk up the resolution scope chain
        var scope = typeReference.ResolutionScope;
        while (scope.Kind == HandleKind.TypeReference)
        {
            var parentReference = reader.GetTypeReference((TypeReferenceHandle)scope);
            typeName = $"{reader.GetString(parentReference.Name)}.{typeName}";
            scope = parentReference.ResolutionScope;
        }
        // Build full name
        var @namespace = reader.GetString(typeReference.Namespace);
        var fullName = ConcatenateNamespaceAndName(@namespace, typeName);
        // Resolve assembly name
        var assemblyName = reader.GetAssemblyReference((AssemblyReferenceHandle)scope).GetAssemblyName();
        // Construct key
        return new CacheKey(assemblyName.FullName, fullName);
    }

    private static string ConcatenateNamespaceAndName(string? @namespace, string name) =>
        string.IsNullOrWhiteSpace(@namespace) ? name : $"{@namespace}.{name}";

    // NOTE: For some reason we had to disregard public key token, otherwise some weird type referencing
    // case in the REPL with lists threw an exception
    // TODO: Could it be that we don't write the public key token in the type refs we use?
    private static bool AssemblyNamesEqual(AssemblyName a, AssemblyName b) =>
           a.Name == b.Name
        && a.Version == b.Version;
}
