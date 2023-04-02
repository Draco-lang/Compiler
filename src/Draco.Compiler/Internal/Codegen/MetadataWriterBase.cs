using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Utility for writing metadata.
/// </summary>
internal abstract class MetadataWriterBase
{
    /// <summary>
    /// The bytes for the MS public key token.
    /// </summary>
    public static byte[] MicrosoftPublicKeyTokenBytes { get; } = new byte[]
    {
        0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a
    };

    /// <summary>
    /// The underlying metadata builder.
    /// </summary>
    public MetadataBuilder MetadataBuilder { get; } = new();

    /// <summary>
    /// The handle for the written module.
    /// </summary>
    public ModuleDefinitionHandle ModuleDefinitionHandle { get; private set; }

    /// <summary>
    /// The handle for the written assembly.
    /// </summary>
    public AssemblyDefinitionHandle AssemblyDefinitionHandle { get; private set; }

    /// <summary>
    /// Utility for the MS public key token handle.
    /// </summary>
    public BlobHandle MicrosoftPublicKeyToken { get; }

    public MetadataWriterBase(string assemblyName)
    {
        this.WriteModuleAndAssemblyDefinition(assemblyName);

        this.MicrosoftPublicKeyToken = this.MetadataBuilder.GetOrAddBlob(MicrosoftPublicKeyTokenBytes);
    }

    /// <summary>
    /// Defines the module and the assembly within for the written binary.
    /// Also defines the special <Module> type.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly.</param>
    private void WriteModuleAndAssemblyDefinition(string assemblyName)
    {
        var moduleName = Path.ChangeExtension(assemblyName, ".dll");
        this.ModuleDefinitionHandle = this.MetadataBuilder.AddModule(
            generation: 0,
            moduleName: this.MetadataBuilder.GetOrAddString(moduleName),
            // TODO: Proper module-version ID
            mvid: this.MetadataBuilder.GetOrAddGuid(Guid.NewGuid()),
            // TODO: What are these? Encryption?
            encId: default,
            encBaseId: default);
        this.AssemblyDefinitionHandle = this.MetadataBuilder.AddAssembly(
            name: this.MetadataBuilder.GetOrAddString(assemblyName),
            // TODO: Proper versioning
            version: new Version(1, 0, 0, 0),
            culture: default,
            publicKey: default,
            flags: default,
            hashAlgorithm: AssemblyHashAlgorithm.None);

        // Create type definition for the special <Module> type that holds global functions
        // Note, that we don't use that for our free-functions
        this.MetadataBuilder.AddTypeDefinition(
            attributes: default,
            @namespace: default,
            name: this.MetadataBuilder.GetOrAddString("<Module>"),
            baseType: default,
            fieldList: MetadataTokens.FieldDefinitionHandle(1),
            methodList: MetadataTokens.MethodDefinitionHandle(1));
    }

    protected AssemblyReferenceHandle AddAssemblyReference(string name) => this.MetadataBuilder.AddAssemblyReference(
        name: this.MetadataBuilder.GetOrAddString(name),
        // TODO: What version?
        version: new Version(1, 0),
        culture: default,
        // TODO: We only need to think about it when we decide to support strong naming
        // Apparently it's not really present in Core
        publicKeyOrToken: default,
        flags: default,
        hashValue: default);

    protected TypeReferenceHandle AddTypeReference(
        AssemblyReferenceHandle assembly,
        string @namespace,
        string name) => this.MetadataBuilder.AddTypeReference(
            resolutionScope: assembly,
            @namespace: this.MetadataBuilder.GetOrAddString(@namespace),
            name: this.MetadataBuilder.GetOrAddString(name));

    protected MemberReferenceHandle AddMethodReference(
        TypeReferenceHandle type,
        string name,
        Action<MethodSignatureEncoder> signature)
    {
        var signatureBuilder = new BlobBuilder();
        var signatureEncoder = new BlobEncoder(signatureBuilder).MethodSignature();
        signature(signatureEncoder);
        return this.MetadataBuilder.AddMemberReference(
            parent: type,
            name: this.MetadataBuilder.GetOrAddString(name),
            signature: this.MetadataBuilder.GetOrAddBlob(signatureBuilder));
    }
}
