using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
    private readonly Dictionary<Uri, DocumentHandle> documentHandles = new();

    // Local state
    private int parameterIndex = 1;

    protected ParameterHandle NextParameterHandle => MetadataTokens.ParameterHandle(this.parameterIndex);

    // Basic get-or-add

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

    // Abstractions for creation

    protected ModuleDefinitionHandle AddModuleDefinition(
        int generation,
        string name,
        Guid moduleVersionId) => this.MetadataBuilder.AddModule(
            generation: generation,
            moduleName: this.GetOrAddString(name),
            mvid: this.GetOrAddGuid(moduleVersionId),
            encId: default,
            encBaseId: default);

    protected AssemblyDefinitionHandle AddAssemblyDefinition(
        string name,
        Version version) => this.MetadataBuilder.AddAssembly(
            name: this.GetOrAddString(name),
            version: version,
            culture: default,
            publicKey: default,
            flags: default,
            hashAlgorithm: AssemblyHashAlgorithm.None);

    protected TypeDefinitionHandle AddTypeDefinition(
        TypeAttributes attributes,
        string? @namespace,
        string name,
        TypeReferenceHandle baseType,
        FieldDefinitionHandle fieldList,
        MethodDefinitionHandle methodList) => this.MetadataBuilder.AddTypeDefinition(
            attributes: attributes,
            @namespace: this.GetOrAddString(@namespace),
            name: this.GetOrAddString(name),
            baseType: baseType,
            fieldList: fieldList,
            methodList: methodList);

    protected FieldDefinitionHandle AddFieldDefinition(
        FieldAttributes attributes,
        string name,
        BlobHandle signature) => this.MetadataBuilder.AddFieldDefinition(
            attributes: attributes,
            name: this.GetOrAddString(name),
            signature: signature);

    protected ParameterHandle AddParameterDefinition(
        ParameterAttributes attributes,
        string name,
        int index)
    {
        var result = this.MetadataBuilder.AddParameter(
            attributes: attributes,
            name: this.GetOrAddString(name),
            sequenceNumber: index + 1);
        ++this.parameterIndex;
        return result;
    }

    protected CustomAttributeHandle AddAttribute(
        EntityHandle target,
        MemberReferenceHandle ctor,
        BlobHandle value) => this.MetadataBuilder.AddCustomAttribute(
            parent: target,
            constructor: ctor,
            value: value);

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

    // Abstractions for debug info

    protected DocumentHandle GetOrAddDocument(Uri documentPath, Guid languageGuid)
    {
        if (!this.documentHandles.TryGetValue(documentPath, out var handle))
        {
            var documentName = this.MetadataBuilder.GetOrAddDocumentName(documentPath.AbsolutePath);
            handle = this.MetadataBuilder.AddDocument(
                name: documentName,
                hashAlgorithm: default,
                hash: default,
                language: this.GetOrAddGuid(languageGuid));
            this.documentHandles.Add(documentPath, handle);
        }
        return handle;
    }

    // From: https://github.com/dotnet/roslyn/blob/723b5ef7fc8146c65993814f1dba94f55f1c59a6/src/Compilers/Core/Portable/PEWriter/MetadataWriter.PortablePdb.cs#L618
    protected BlobHandle AddSequencePoints(
        StandaloneSignatureHandle localSignatureHandleOpt,
        ImmutableArray<SequencePoint> sequencePoints)
    {
        // No-op
        if (sequencePoints.Length == 0) return default;

        var writer = new BlobBuilder();

        var previousNonHiddenStartLine = -1;
        var previousNonHiddenStartColumn = -1;

        // Header
        writer.WriteCompressedInteger(MetadataTokens.GetRowNumber(localSignatureHandleOpt));

        // First document
        var previousDocument = sequencePoints[0].Document;

        // Go through sequence points
        for (var i = 0; i < sequencePoints.Length; i++)
        {
            var currentDocument = sequencePoints[i].Document;
            if (currentDocument != previousDocument)
            {
                writer.WriteCompressedInteger(0);
                writer.WriteCompressedInteger(MetadataTokens.GetRowNumber(currentDocument));
                previousDocument = currentDocument;
            }

            // Delta IL offset
            if (i > 0)
            {
                writer.WriteCompressedInteger(sequencePoints[i].IlOffset - sequencePoints[i - 1].IlOffset);
            }
            else
            {
                writer.WriteCompressedInteger(sequencePoints[i].IlOffset);
            }

            if (sequencePoints[i].IsHidden)
            {
                writer.WriteInt16(0);
                continue;
            }

            // Delta Lines & Columns
            EncodeDeltaLinesAndColumns(writer, sequencePoints[i]);

            // Delta Start Lines & Columns:
            if (previousNonHiddenStartLine < 0)
            {
                Debug.Assert(previousNonHiddenStartColumn < 0);
                writer.WriteCompressedInteger(sequencePoints[i].StartLine);
                writer.WriteCompressedInteger(sequencePoints[i].StartColumn);
            }
            else
            {
                writer.WriteCompressedSignedInteger(sequencePoints[i].StartLine - previousNonHiddenStartLine);
                writer.WriteCompressedSignedInteger(sequencePoints[i].StartColumn - previousNonHiddenStartColumn);
            }

            previousNonHiddenStartLine = sequencePoints[i].StartLine;
            previousNonHiddenStartColumn = sequencePoints[i].StartColumn;
        }

        return this.GetOrAddBlob(writer);
    }

    private static void EncodeDeltaLinesAndColumns(BlobBuilder writer, SequencePoint sequencePoint)
    {
        var deltaLines = sequencePoint.EndLine - sequencePoint.StartLine;
        var deltaColumns = sequencePoint.EndColumn - sequencePoint.StartColumn;

        // Only hidden sequence points have zero width
        Debug.Assert(deltaLines != 0 || deltaColumns != 0 || sequencePoint.IsHidden);

        writer.WriteCompressedInteger(deltaLines);

        if (deltaLines == 0)
        {
            writer.WriteCompressedInteger(deltaColumns);
        }
        else
        {
            writer.WriteCompressedSignedInteger(deltaColumns);
        }
    }
}
