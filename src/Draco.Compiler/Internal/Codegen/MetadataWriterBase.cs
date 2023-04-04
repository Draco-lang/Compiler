using System;
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

    protected StringHandle GetOrAddString(string? text) => text is null
        ? default
        : this.MetadataBuilder.GetOrAddString(text);
    protected GuidHandle GetOrAddGuid(Guid guid) => this.MetadataBuilder.GetOrAddGuid(guid);
    protected BlobHandle GetOrAddBlob(BlobBuilder blob) => this.MetadataBuilder.GetOrAddBlob(blob);

    protected AssemblyReferenceHandle AddAssemblyReference(
        string name,
        Version version,
        BlobHandle publicKeyOrToken = default) => this.MetadataBuilder.AddAssemblyReference(
        name: this.GetOrAddString(name),
        // TODO: What version?
        version: version,
        culture: default,
        // TODO: We only need to think about it when we decide to support strong naming
        // Apparently it's not really present in Core
        publicKeyOrToken: publicKeyOrToken,
        flags: default,
        hashValue: default);

    protected TypeReferenceHandle AddTypeReference(
        ModuleDefinitionHandle module,
        string? @namespace,
        string name) => this.MetadataBuilder.AddTypeReference(
            resolutionScope: module,
            @namespace: this.GetOrAddString(@namespace),
            name: this.GetOrAddString(name));

    protected TypeReferenceHandle AddTypeReference(
        AssemblyReferenceHandle assembly,
        string? @namespace,
        string name) => this.MetadataBuilder.AddTypeReference(
            resolutionScope: assembly,
            @namespace: this.GetOrAddString(@namespace),
            name: this.GetOrAddString(name));

    protected TypeReferenceHandle AddTypeReference(
        TypeReferenceHandle containingType,
        string? @namespace,
        string name) => this.MetadataBuilder.AddTypeReference(
            resolutionScope: containingType,
            @namespace: this.GetOrAddString(@namespace),
            name: this.GetOrAddString(name));

    protected MemberReferenceHandle AddMethodReference(
        TypeReferenceHandle type,
        string name,
        BlobHandle signature) => this.MetadataBuilder.AddMemberReference(
            parent: type,
            name: this.GetOrAddString(name),
            signature: signature);

    protected MemberReferenceHandle AddMethodReference(
        TypeReferenceHandle type,
        string name,
        Action<MethodSignatureEncoder> signature)
    {
        var signatureBuilder = new BlobBuilder();
        var signatureEncoder = new BlobEncoder(signatureBuilder).MethodSignature();
        signature(signatureEncoder);
        return this.AddMethodReference(
            type: type,
            name: name,
            signature: this.GetOrAddBlob(signatureBuilder));
    }
}
