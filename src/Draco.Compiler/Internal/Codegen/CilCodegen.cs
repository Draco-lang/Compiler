using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.DracoIr;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Generates CIL from DracoIR.
/// </summary>
internal sealed class CilCodegen
{
    /// <summary>
    /// Generates a PE file from the given Draco assembly.
    /// </summary>
    /// <param name="assembly">The assembly to generate an executable from.</param>
    /// <param name="peStream">The stream to write the executable to.</param>
    public static void Generate(IReadOnlyAssembly assembly, Stream peStream)
    {
        var codegen = new CilCodegen(assembly);

        codegen.CreateModuleAndAssembly();
        codegen.CreateFreeFunctionsClass();
        codegen.GeneratePe(peStream);
    }

    private readonly IReadOnlyAssembly assembly;

    private readonly MetadataBuilder metadataBuilder = new();
    private readonly BlobBuilder ilBuilder = new();

    private CilCodegen(IReadOnlyAssembly assembly)
    {
        this.assembly = assembly;
    }

    public void CreateModuleAndAssembly()
    {
        this.metadataBuilder.AddModule(
            generation: 0,
            moduleName: this.metadataBuilder.GetOrAddString(this.assembly.Name),
            // TODO: Proper module-version ID
            mvid: this.metadataBuilder.GetOrAddGuid(Guid.NewGuid()),
            // TODO: What are these? Encryption?
            encId: default,
            encBaseId: default);

        this.metadataBuilder.AddAssembly(
            name: this.metadataBuilder.GetOrAddString(this.assembly.Name),
            // TODO: Proper versioning
            version: new Version(1, 0, 0, 0),
            culture: default,
            publicKey: default,
            flags: 0,
            hashAlgorithm: AssemblyHashAlgorithm.None);
    }

    public void CreateFreeFunctionsClass()
    {
        this.metadataBuilder.AddTypeDefinition(
            attributes: TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit,
            @namespace: default,
            name: this.metadataBuilder.GetOrAddString("FreeFunctions"),
            // TODO: System.Object
            baseType: default,
            // TODO: What's this?
            fieldList: MetadataTokens.FieldDefinitionHandle(1),
            // TODO: What's this?
            methodList: MetadataTokens.MethodDefinitionHandle(1));
    }

    public void GeneratePe(Stream peStream)
    {
        var peHeaderBuilder = new PEHeaderBuilder(
            imageCharacteristics: Characteristics.Dll);
        var peBuilder = new ManagedPEBuilder(
            header: peHeaderBuilder,
            metadataRootBuilder: new(this.metadataBuilder),
            ilStream: this.ilBuilder,
            // TODO: When entry point is exposed from assembly
            entryPoint: default,
            flags: CorFlags.ILOnly,
            // TODO: For deterministic builds
            deterministicIdProvider: null);

        var peBlob = new BlobBuilder();
        var contentId = peBuilder.Serialize(peBlob);
        peBlob.WriteContentTo(peStream);
    }
}
