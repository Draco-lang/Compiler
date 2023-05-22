using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Debugger;

/// <summary>
/// Represents debug information of a method.
/// </summary>
internal sealed class MethodInfo
{
    /// <summary>
    /// The method definition handle.
    /// </summary>
    public MethodDefinition Definition { get; }

    /// <summary>
    /// The method debug information handle.
    /// </summary>
    public MethodDebugInformation DebugInfo { get; }

    /// <summary>
    /// The document the method is defined in.
    /// </summary>
    public DocumentHandle Document => this.DebugInfo.Document;

    private readonly MetadataReader pdbReader;

    public MethodInfo(
        MetadataReader pdbReader,
        MethodDefinition definition,
        MethodDebugInformation debugInfo)
    {
        this.Definition = definition;
        this.DebugInfo = debugInfo;
        this.pdbReader = pdbReader;
    }
}
