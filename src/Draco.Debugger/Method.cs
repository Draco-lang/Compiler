using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// Represents a method.
/// </summary>
public sealed class Method
{
    /// <summary>
    /// The cache for this object.
    /// </summary>
    internal SessionCache SessionCache { get; }

    /// <summary>
    /// The internal handle.
    /// </summary>
    internal CorDebugFunction CorDebugFunction { get; }

    /// <summary>
    /// The method definition handle of this method.
    /// </summary>
    internal MethodDefinitionHandle MethodDefinitionHandle => MetadataTokens.MethodDefinitionHandle(this.CorDebugFunction.Token);

    /// <summary>
    /// The debug info of this method.
    /// </summary>
    internal MethodDebugInformation DebugInfo => this.debugInfo ??= this.BuildDebugInfo();
    private MethodDebugInformation? debugInfo;

    /// <summary>
    /// The module this function lies in.
    /// </summary>
    public Module Module => this.SessionCache.GetModule(this.CorDebugFunction.Module);

    /// <summary>
    /// The name of the method.
    /// </summary>
    public string Name => this.name ??= this.BuildName();
    private string? name;

    /// <summary>
    /// The source file this method lies in.
    /// </summary>
    public SourceFile? SourceFile => this.sourceFile ??= this.BuildSourceFile();
    private SourceFile? sourceFile;

    /// <summary>
    /// The sequence points within this method.
    /// </summary>
    public ImmutableArray<SequencePoint> SequencePoints => this.sequencePoints ??= this.BuildSequencePoints();
    private ImmutableArray<SequencePoint>? sequencePoints;

    /// <summary>
    /// The breakpoints within this method.
    /// </summary>
    public ImmutableArray<Breakpoint> Breakpoints => this.MutableBreakpoints.ToImmutableArray();
    internal HashSet<Breakpoint> MutableBreakpoints = new();

    internal Method(SessionCache sessionCache, CorDebugFunction corDebugFunction)
    {
        this.SessionCache = sessionCache;
        this.CorDebugFunction = corDebugFunction;
    }

    /// <summary>
    /// Attempts to place a breakpoint in this method.
    /// </summary>
    /// <param name="position">The position to place the breakpoint at.</param>
    /// <param name="breakpoint">The placed breakpoint, if any.</param>
    /// <returns>True, if the breakpoint was successfully placed, false otherwise.</returns>
    public bool TryPlaceBreakpoint(SourcePosition position, [MaybeNullWhen(false)] out Breakpoint breakpoint)
    {
        var offset = this.GetOffsetForSourcePosition(position);
        if (offset is not null)
        {
            var corDebugBreakpoint = this.CorDebugFunction.ILCode.CreateBreakpoint(offset.Value);
            breakpoint = this.SessionCache.GetBreakpoint(corDebugBreakpoint);
            return true;
        }
        else
        {
            breakpoint = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to place a breakpoint in this method.
    /// </summary>
    /// <param name="line">The line to place the breakpoint at.</param>
    /// <param name="breakpoint">The placed breakpoint, if any.</param>
    /// <returns>True, if the breakpoint was successfully placed, false otherwise.</returns>
    public bool TryPlaceBreakpoint(int line, [MaybeNullWhen(false)] out Breakpoint breakpoint)
    {
        var offset = this.GetOffsetForLine(line);
        if (offset is not null)
        {
            var corDebugBreakpoint = this.CorDebugFunction.ILCode.CreateBreakpoint(offset.Value);
            breakpoint = this.SessionCache.GetBreakpoint(corDebugBreakpoint);
            return true;
        }
        else
        {
            breakpoint = null;
            return false;
        }
    }

    internal int? GetOffsetForLine(int line)
    {
        if (this.SequencePoints.Length == 0) return null;
        if (line < this.SequencePoints[0].GetStartPosition().Line
         || line > this.SequencePoints[^1].GetEndPosition().Line) return null;

        // NOTE: A binary search would be more beneficial
        var seqPoint = this.SequencePoints.FirstOrDefault(s => s.StartLine == line);
        return seqPoint.Document.IsNil
            ? null
            : seqPoint.Offset;
    }

    internal int? GetOffsetForSourcePosition(SourcePosition position)
    {
        if (this.SequencePoints.Length == 0) return null;
        if (position < this.SequencePoints[0].GetStartPosition()
         || position > this.SequencePoints[^1].GetEndPosition()) return null;

        // NOTE: A binary search would be more beneficial
        var seqPoint = this.SequencePoints.FirstOrDefault(s => s.Contains(position));
        return seqPoint.Document.IsNil
            ? null
            : seqPoint.Offset;
    }

    internal SourceRange? GetSourceRangeForIlOffset(int offset)
    {
        if (this.SequencePoints.Length == 0) return null;
        if (offset < this.SequencePoints[0].Offset || offset > this.SequencePoints[^1].Offset) return null;

        // Default to the last one
        var seqPoint = this.SequencePoints[^1];
        // NOTE: A binary search would be more beneficial
        for (var i = 0; i < this.SequencePoints.Length - 1; ++i)
        {
            var curr = this.SequencePoints[i];
            var next = this.SequencePoints[i + 1];
            if (curr.Offset <= offset && offset < next.Offset)
            {
                seqPoint = curr;
                break;
            }
        }

        return seqPoint.Document.IsNil || seqPoint.IsHidden
            ? null
            : new(
                startLine: seqPoint.StartLine - 1,
                startColumn: seqPoint.StartColumn - 1,
                endLine: seqPoint.EndLine - 1,
                endColumn: seqPoint.EndColumn);
    }

    private MethodDebugInformation BuildDebugInfo() => this.Module.PdbReader
        .GetMethodDebugInformation(this.MethodDefinitionHandle);

    private string BuildName()
    {
        var import = this.CorDebugFunction.Module.GetMetaDataInterface().MetaDataImport;
        var methodProps = import.GetMethodProps(this.CorDebugFunction.Token);
        return methodProps.szMethod;
    }

    private SourceFile? BuildSourceFile()
    {
        var module = this.Module;
        var docHandle = this.DebugInfo.Document;
        return module.SourceFiles.FirstOrDefault(s => s.DocumentHandle == docHandle);
    }

    private ImmutableArray<SequencePoint> BuildSequencePoints() => this.DebugInfo
        .GetSequencePoints()
        .ToImmutableArray();
}
