using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Draco.Debugger;

/// <summary>
/// Represents debug information of a method.
/// </summary>
internal sealed class MethodInfo
{
    /// <summary>
    /// The method definition handle.
    /// </summary>
    public MethodDefinitionHandle DefinitionHandle { get; }

    /// <summary>
    /// The method debug information.
    /// </summary>
    public MethodDebugInformation DebugInfo { get; }

    /// <summary>
    /// The document the method is defined in.
    /// </summary>
    public DocumentHandle DocumentHandle => this.DebugInfo.Document;

    /// <summary>
    /// The sequence points within this method.
    /// </summary>
    public ImmutableArray<SequencePoint> SequencePoints => this.sequencePoints ??= this.BuildSequencePoints();
    private ImmutableArray<SequencePoint>? sequencePoints;

    public MethodInfo(
        MethodDefinitionHandle definitionHandle,
        MethodDebugInformation debugInfo)
    {
        this.DefinitionHandle = definitionHandle;
        this.DebugInfo = debugInfo;
    }

    private ImmutableArray<SequencePoint> BuildSequencePoints() => this.DebugInfo
        .GetSequencePoints()
        .ToImmutableArray();
}
