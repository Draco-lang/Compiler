using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Utility for writing metadata.
/// </summary>
internal abstract class MetadataWriterBase
{
    /// <summary>
    /// The underlying metadata builder.
    /// </summary>
    public MetadataBuilder MetadataBuilder { get; } = new();

    // Cache
    private readonly Dictionary<(string Name, Version Version), AssemblyReferenceHandle> assemblyReferences = new();
    private readonly Dictionary<(EntityHandle Parent, string Namespace, string Name), TypeReferenceHandle> typeReferences = new();

    protected StringHandle GetOrAddString(string? text) => text is null
        ? default
        : this.MetadataBuilder.GetOrAddString(text);
    protected GuidHandle GetOrAddGuid(Guid guid) => this.MetadataBuilder.GetOrAddGuid(guid);
    protected BlobHandle GetOrAddBlob(BlobBuilder blob) => this.MetadataBuilder.GetOrAddBlob(blob);
    protected BlobHandle GetOrAddBlob(byte[] blob) => this.MetadataBuilder.GetOrAddBlob(blob);
    protected BlobHandle EncodeBlob(Action<BlobEncoder> encoder)
    {
        var blob = new BlobBuilder();
        encoder(new BlobEncoder(blob));
        return this.GetOrAddBlob(blob);
    }

    // Abstraction for references

    protected AssemblyReferenceHandle GetOrAddAssemblyReference(
        string name,
        Version version,
        byte[]? publicKeyOrToken = null)
    {
        if (!this.assemblyReferences.TryGetValue((name, version), out var handle))
        {
            handle = this.MetadataBuilder.AddAssemblyReference(
                name: this.GetOrAddString(name),
                version: version,
                culture: default,
                publicKeyOrToken: publicKeyOrToken is null ? default : this.GetOrAddBlob(publicKeyOrToken),
                flags: default,
                hashValue: default);
            this.assemblyReferences.Add((name, version), handle);
        }
        return handle;
    }

    private TypeReferenceHandle GetOrAddTypeReference(
        EntityHandle parent,
        string? @namespace,
        string name) => this.MetadataBuilder.AddTypeReference(
            resolutionScope: parent,
            @namespace: this.GetOrAddString(@namespace),
            name: this.GetOrAddString(name));

    protected TypeReferenceHandle GetOrAddTypeReference(
        AssemblyReferenceHandle assembly,
        string? @namespace,
        string name) => this.GetOrAddTypeReference(
            parent: assembly,
            @namespace: @namespace,
            name: name);

    protected TypeReferenceHandle GetOrAddTypeReference(
        ModuleDefinitionHandle module,
        string? @namespace,
        string name) => this.GetOrAddTypeReference(
            parent: module,
            @namespace: @namespace,
            name: name);

    protected TypeReferenceHandle GetOrAddTypeReference(
        TypeReferenceHandle containingType,
        string? @namespace,
        string name) => this.GetOrAddTypeReference(
            parent: containingType,
            @namespace: @namespace,
            name: name);

    // NOTE: non-caching, we don't deal with memoizing signatures
    protected MemberReferenceHandle AddMemberReference(
        TypeReferenceHandle type,
        string name,
        BlobHandle signature) => this.MetadataBuilder.AddMemberReference(
            parent: type,
            name: this.GetOrAddString(name),
            signature: signature);
}
