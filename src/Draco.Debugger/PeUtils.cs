using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Debugger;

/// <summary>
/// Utilities for PE files.
/// </summary>
internal static class PeUtils
{
    /// <summary>
    /// Retrieves the entry point handle from the given PE file reader.
    /// </summary>
    /// <param name="reader">The reader for the PE file.</param>
    /// <returns>The <see cref="MethodDefinitionHandle"/> of the entry point. Can be a nil handle,
    /// if the PE does not have a COR header or the executable has a native entry point.</returns>
    public static MethodDefinitionHandle GetEntryPoint(this PEReader reader)
    {
        var corHeader = reader.PEHeaders.CorHeader;
        if (corHeader is null) return default;
        if (corHeader.Flags.HasFlag(CorFlags.NativeEntryPoint)) return default;
        return MetadataTokens.MethodDefinitionHandle(corHeader.EntryPointTokenOrRelativeVirtualAddress);
    }
}
