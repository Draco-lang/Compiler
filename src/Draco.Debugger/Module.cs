using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// Represents a module loaded by the runtime.
/// </summary>
public sealed class Module
{
    /// <summary>
    /// The cache for this object.
    /// </summary>
    internal SessionCache SessionCache { get; }

    /// <summary>
    /// The native representation of the module.
    /// </summary>
    internal CorDebugModule CorDebugModule { get; }

    /// <summary>
    /// The name of this module.
    /// </summary>
    public string Name => this.CorDebugModule.Name;

    /// <summary>
    /// The name of this module's corresponding PDB file.
    /// </summary>
    public string PdbName => Path.ChangeExtension(this.Name, ".pdb");

    /// <summary>
    /// The reader for this module's PE file.
    /// </summary>
    public PEReader PeReader
    {
        get
        {
            if (this.peReader is not null) return this.peReader;
            var peStream = File.OpenRead(this.Name);
            this.peReader = new PEReader(peStream);
            return this.peReader;
        }
    }
    private PEReader? peReader;

    /// <summary>
    /// The source files corresponding to this module.
    /// </summary>
    public ImmutableArray<SourceFile> SourceFiles => this.sourceFiles ??= this.BuildSourceFiles();
    private ImmutableArray<SourceFile>? sourceFiles;

    /// <summary>
    /// The reader for this module's PDB.
    /// </summary>
    public MetadataReader PdbReader
    {
        get
        {
            if (this.pdbReaderProvider is not null) return this.pdbReaderProvider.GetMetadataReader();
            var pdbStream = File.OpenRead(this.PdbName);
            this.pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            return this.pdbReaderProvider.GetMetadataReader();
        }
    }
    private MetadataReaderProvider? pdbReaderProvider;

    internal Module(SessionCache sessionCache, CorDebugModule corDebugModule)
    {
        this.SessionCache = sessionCache;
        this.CorDebugModule = corDebugModule;
    }

    private ImmutableArray<SourceFile> BuildSourceFiles()
    {
        var reader = this.PdbReader;
        return reader.Documents
            .Select(docHandle =>
            {
                var doc = reader.GetDocument(docHandle);
                var docPath = reader.GetString(doc.Name);
                return new SourceFile(this, docHandle, new Uri(docPath));
            })
            .ToImmutableArray();
    }
}
