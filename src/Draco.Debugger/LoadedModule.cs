using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;
using Draco.Debugger.IO;

namespace Draco.Debugger;

/// <summary>
/// Represents a module loaded by the runtime.
/// </summary>
internal sealed class LoadedModule
{
    /// <summary>
    /// The native representation of the module.
    /// </summary>
    public CorDebugModule CorDebugModule { get; }

    /// <summary>
    /// The name of this module.
    /// </summary>
    public string Name => this.CorDebugModule.Name;

    /// <summary>
    /// The reader for this module's PE file.
    /// </summary>
    public PEReader PeReader
    {
        get
        {
            if (this.peReader is not null) return this.peReader;
            var baseAddr = (nint)this.CorDebugModule.BaseAddress.Value;
            var peStream = new MemoryAddressStream(baseAddr, this.CorDebugModule.Size);
            this.peReader = new PEReader(peStream);
            return this.peReader;
        }
    }
    private PEReader? peReader;

    /// <summary>
    /// The reader for this module's PDB.
    /// </summary>
    public MetadataReader PdbReader
    {
        get
        {
            if (this.pdbReaderProvider is not null) return this.pdbReaderProvider.GetMetadataReader();

            // TODO
            throw new NotImplementedException();
        }
    }
    private MetadataReaderProvider? pdbReaderProvider;

    public LoadedModule(CorDebugModule corDebugModule)
    {
        this.CorDebugModule = corDebugModule;
    }
}
