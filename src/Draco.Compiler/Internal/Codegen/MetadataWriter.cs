using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Utility base class for writing metadata.
/// </summary>
internal abstract class MetadataWriter
{
    /// <summary>
    /// The underlying metadata builder.
    /// </summary>
    public MetadataBuilder MetadataBuilder { get; } = new();

    // Cache
    private readonly Dictionary<(string Name, Version Version), AssemblyReferenceHandle> assemblyReferences = new();
    private readonly Dictionary<(EntityHandle Parent, string? Namespace, string Name), TypeReferenceHandle> typeReferences = new();
    private readonly Dictionary<Uri, DocumentHandle> documentHandles = new();

    // Local state
    private int parameterIndex = 1;
    private int localVariableIndex = 1;

    protected ParameterHandle NextParameterHandle => MetadataTokens.ParameterHandle(this.parameterIndex);
    protected LocalVariableHandle NextLocalVariableHandle => MetadataTokens.LocalVariableHandle(this.localVariableIndex);

    // Basic get-or-add

    public StringHandle GetOrAddString(string? text) => text is null
        ? default
        : this.MetadataBuilder.GetOrAddString(text);
    public GuidHandle GetOrAddGuid(Guid guid) => this.MetadataBuilder.GetOrAddGuid(guid);
    public BlobHandle GetOrAddBlob(BlobBuilder blob) => this.MetadataBuilder.GetOrAddBlob(blob);
    public BlobHandle GetOrAddBlob(byte[] blob) => this.MetadataBuilder.GetOrAddBlob(blob);
    public BlobHandle EncodeBlob(Action<BlobEncoder> encoder)
    {
        var blob = new BlobBuilder();
        encoder(new BlobEncoder(blob));
        return this.GetOrAddBlob(blob);
    }

    // Abstractions for creation

    public ModuleDefinitionHandle AddModuleDefinition(
        int generation,
        string name,
        Guid moduleVersionId) => this.MetadataBuilder.AddModule(
            generation: generation,
            moduleName: this.GetOrAddString(name),
            mvid: this.GetOrAddGuid(moduleVersionId),
            encId: default,
            encBaseId: default);

    public AssemblyDefinitionHandle AddAssemblyDefinition(
        string name,
        Version version) => this.MetadataBuilder.AddAssembly(
            name: this.GetOrAddString(name),
            version: version,
            culture: default,
            publicKey: default,
            flags: default,
            hashAlgorithm: AssemblyHashAlgorithm.None);

    public TypeDefinitionHandle AddTypeDefinition(
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

    public FieldDefinitionHandle AddFieldDefinition(
        FieldAttributes attributes,
        string name,
        BlobHandle signature) => this.MetadataBuilder.AddFieldDefinition(
            attributes: attributes,
            name: this.GetOrAddString(name),
            signature: signature);

    public ParameterHandle AddParameterDefinition(
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

    public CustomAttributeHandle AddAttribute(
        EntityHandle target,
        MemberReferenceHandle ctor,
        BlobHandle value) => this.MetadataBuilder.AddCustomAttribute(
            parent: target,
            constructor: ctor,
            value: value);

    // Abstraction for references

    public AssemblyReferenceHandle GetOrAddAssemblyReference(
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

    public TypeReferenceHandle GetOrAddTypeReference(
        EntityHandle parent,
        string? @namespace,
        string name)
    {
        if (!this.typeReferences.TryGetValue((parent, @namespace, name), out var handle))
        {
            handle = this.MetadataBuilder.AddTypeReference(
                resolutionScope: parent,
                @namespace: @namespace is null
                    ? default
                    : this.GetOrAddString(@namespace),
                name: this.GetOrAddString(name));
            this.typeReferences.Add((parent, @namespace, name), handle);
        }
        return handle;
    }

    public TypeReferenceHandle GetOrAddTypeReference(
        AssemblyReferenceHandle assembly,
        string? @namespace,
        string name) => this.GetOrAddTypeReference(
            parent: assembly,
            @namespace: @namespace,
            name: name);

    public TypeReferenceHandle GetOrAddTypeReference(
        ModuleDefinitionHandle module,
        string? @namespace,
        string name) => this.GetOrAddTypeReference(
            parent: module,
            @namespace: @namespace,
            name: name);

    public TypeReferenceHandle GetOrAddTypeReference(
        ModuleReferenceHandle module,
        string? @namespace,
        string name) => this.GetOrAddTypeReference(
            parent: module,
            @namespace: @namespace,
            name: name);

    public TypeReferenceHandle GetOrAddTypeReference(
        TypeReferenceHandle containingType,
        string? @namespace,
        string name) => this.GetOrAddTypeReference(
            parent: containingType,
            @namespace: @namespace,
            name: name);

    // NOTE: non-caching, we don't deal with memoizing signatures
    public MemberReferenceHandle AddMemberReference(
        EntityHandle parent,
        string name,
        BlobHandle signature) => this.MetadataBuilder.AddMemberReference(
            parent: parent,
            name: this.GetOrAddString(name),
            signature: signature);

    // Abstractions for debug info

    public DocumentHandle GetOrAddDocument(Uri documentPath, Guid languageGuid)
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

    public LocalVariableHandle AddLocalVariable(
        LocalVariableAttributes attributes,
        int index,
        string name)
    {
        var handle = this.MetadataBuilder.AddLocalVariable(
            attributes: attributes,
            index: index,
            name: this.GetOrAddString(name));
        ++this.localVariableIndex;
        return handle;
    }

    // From: https://github.com/dotnet/roslyn/blob/723b5ef7fc8146c65993814f1dba94f55f1c59a6/src/Compilers/Core/Portable/PEWriter/MetadataWriter.PortablePdb.cs#L618
    public BlobHandle AddSequencePoints(
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
        for (var i = 0; i < sequencePoints.Length; ++i)
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
