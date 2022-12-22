using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Draco.Compiler.Internal.DracoIr;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Emitter for a DracoIR assembly to CIL.
/// </summary>
internal sealed class CilCodegen
{
    // TODO: Allow for some method of generating *only* IL instead of combined IL and PE.
    /// <summary>
    /// Emits a DracoIR assembly as CIL and writes it into a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="assembly">The assembly to emit.</param>
    /// <param name="options">The emission options.</param>
    public static void Generate(Stream stream, IReadOnlyAssembly assembly, CilEmissionOptions options)
    {
        var ilBuilder = new BlobBuilder();
        var metadata = new MetadataBuilder();
        var codegen = new CilCodegen(assembly, options, ilBuilder, metadata);

        codegen.Emit();

        var entryPoint = default(MethodDefinitionHandle);
        if (options.Kind == EmissionKind.Executable)
        {
            // TODO: Replace this with some API for getting the assembly entry point.
            var main = assembly.Procedures.GetValueOrDefault("main")
                ?? throw new InvalidOperationException("The assembly does not have an entry point.");
            entryPoint = codegen.prodecureDefinitions[main];
        }

        codegen.WriteToStream(stream, entryPoint);
    }

    private readonly IReadOnlyAssembly assembly;
    private readonly CilEmissionOptions options;
    private readonly BlobBuilder ilBuilder;
    private readonly MetadataBuilder metadata;
    private readonly ReservedBlob<GuidHandle> mvidGuidHandle;
    private BlobContentId contentId; // Should be set by Emit().
    private readonly Dictionary<IReadOnlyProcecude, MethodDefinitionHandle> prodecureDefinitions;

    private CilCodegen(
        IReadOnlyAssembly assembly,
        CilEmissionOptions options,
        BlobBuilder ilBuilder,
        MetadataBuilder metadata)
    {
        this.assembly = assembly;
        this.options = options;
        this.ilBuilder = ilBuilder;
        this.metadata = metadata;
        this.mvidGuidHandle = metadata.ReserveGuid();
        this.contentId = default;
        this.prodecureDefinitions = new();
    }

    /// <summary>
    /// Writes the generated CIL to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="entryPoint">The entry point of the assembly.</param>
    private void WriteToStream(Stream stream, MethodDefinitionHandle entryPoint = default)
    {
        var (imageCharacteristics, flags) = this.options.Kind switch
        {
            EmissionKind.Executable => (Characteristics.ExecutableImage, CorFlags.ILOnly),
            EmissionKind.Library => (Characteristics.Dll, CorFlags.ILOnly),
            _ => throw new UnreachableException()
        };

        var peHeaderBuilder = new PEHeaderBuilder(
            imageCharacteristics: imageCharacteristics);

        var peBuilder = new ManagedPEBuilder(
            header: peHeaderBuilder,
            metadataRootBuilder: new MetadataRootBuilder(this.metadata),
            ilStream: this.ilBuilder,
            entryPoint: entryPoint,
            flags: flags,
            deterministicIdProvider: _ => this.contentId);

        var peBlob = new BlobBuilder();
        peBuilder.Serialize(peBlob);
        peBlob.WriteContentTo(stream);
    }

    /// <summary>
    /// Emits the assembly to the metadata and IL builders.
    /// </summary>
    private void Emit()
    {
        var moduleName = this.GetModuleName();

        this.metadata.AddModule(
            0,
            this.metadata.GetOrAddString(moduleName),
            this.mvidGuidHandle.Handle,
            default,
            default);

        this.metadata.AddAssembly(
            this.metadata.GetOrAddString(this.assembly.Name),
            this.options.Version,
            default,
            default,
            default,
            this.options.HashAlgorithm);

        // TODO: Implementation.

        // Use the generated blobs to produce the content ID.
        var blobs = this.ilBuilder.GetBlobs();
        this.contentId = this.options.GetModuleVersionId(blobs);
        this.mvidGuidHandle.CreateWriter().WriteGuid(this.contentId.Guid);
    }

    /// <summary>
    /// Gets the module name for the assembly.
    /// </summary>
    private string GetModuleName()
    {
        var extension = this.options.Kind switch
        {
            EmissionKind.Executable => ".exe",
            EmissionKind.Library => ".dll",
            _ => throw new UnreachableException()
        };

        return $"{this.assembly.Name}{extension}";
    }
}

/// <summary>
/// Options for CIL emission.
/// </summary>
/// <param name="Kind">The kind of assembly to produce.</param>
/// <param name="Version">The version of the the assembly.</param>
/// <param name="HashAlgorithm">The assembly hash algorithm to use.</param>
/// <param name="GetModuleVersionId">A function which produces a content ID from a collection of blobs.
/// This allows for a deterministic module version ID.</param>
internal readonly record struct CilEmissionOptions(
    EmissionKind Kind,
    Version Version,
    AssemblyHashAlgorithm HashAlgorithm,
    Func<IEnumerable<Blob>, BlobContentId> GetModuleVersionId);

/// <summary>
/// An assembly emission kind.
/// </summary>
internal enum EmissionKind
{
    /// <summary>
    /// The assembly should be emitted as an executable with an entry point.
    /// </summary>
    Executable,
    /// <summary>
    /// The assembly should be emitted as a library.
    /// </summary>
    Library
}
