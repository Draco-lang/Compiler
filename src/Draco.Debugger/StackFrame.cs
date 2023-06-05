using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// Represents a single frame in the call-stack.
/// </summary>
public sealed class StackFrame
{
    private static int stackFrameCounter = 0;

    /// <summary>
    /// The cache for this object.
    /// </summary>
    internal SessionCache SessionCache { get; }

    /// <summary>
    /// The internal frame.
    /// </summary>
    internal CorDebugFrame CorDebugFrame { get; }

    /// <summary>
    /// The ID of this stack frame.
    /// </summary>
    public int Id { get; } = Interlocked.Increment(ref stackFrameCounter);

    /// <summary>
    /// The method the frame represents.
    /// </summary>
    public Method Method => this.SessionCache.GetMethod(this.CorDebugFrame.Function);

    /// <summary>
    /// The arguments visible in this frame.
    /// </summary>
    public ImmutableDictionary<string, object?> Arguments => this.arguments ??= this.BuildArguments();
    private ImmutableDictionary<string, object?>? arguments;

    /// <summary>
    /// The locals visible in this frame.
    /// </summary>
    public ImmutableDictionary<string, object?> Locals => this.locals ??= this.BuildLocals();
    private ImmutableDictionary<string, object?>? locals;

    /// <summary>
    /// The range the execution is at.
    /// </summary>
    public SourceRange? Range => this.range ??= this.BuildRange();
    private SourceRange? range;

    internal StackFrame(SessionCache sessionCache, CorDebugFrame corDebugFrame)
    {
        this.SessionCache = sessionCache;
        this.CorDebugFrame = corDebugFrame;
    }

    private SourceRange? BuildRange() => this.CorDebugFrame is CorDebugILFrame ilFrame
        ? this.Method.GetSourceRangeForIlOffset(ilFrame.IP.pnOffset)
        : null;

    private ImmutableDictionary<string, object?> BuildArguments()
    {
        if (this.CorDebugFrame is not CorDebugILFrame ilFrame) return ImmutableDictionary<string, object?>.Empty;

        var meta = this.Method.Module.CorDebugModule.GetMetaDataInterface();
        var methodParams = meta.MetaDataImport.EnumParams(MetadataTokens.GetToken(this.Method.MethodDefinitionHandle));

        var result = ImmutableDictionary.CreateBuilder<string, object?>();

        // Process arguments
        foreach (var paramHandle in methodParams)
        {
            var param = meta.MetaDataImport.GetParamProps(paramHandle);
            var argValue = ilFrame.GetArgument(param.pulSequence - 1);
            result.Add(param.szName, argValue.ToBrowsableObject());
        }

        return result.ToImmutable();
    }

    private ImmutableDictionary<string, object?> BuildLocals()
    {
        if (this.CorDebugFrame is not CorDebugILFrame ilFrame) return ImmutableDictionary<string, object?>.Empty;

        var offset = ilFrame.IP.pnOffset;

        var pdbReader = this.Method.Module.PdbReader;
        var localScopes = pdbReader.GetLocalScopes(this.Method.MethodDefinitionHandle);

        var result = ImmutableDictionary.CreateBuilder<string, object?>();

        // Process locals
        foreach (var scopeHandle in localScopes)
        {
            var scope = pdbReader.GetLocalScope(scopeHandle);
            // Skip if not intersecting our offset
            if (offset < scope.StartOffset || offset > scope.EndOffset) continue;

            // Get locals
            foreach (var localHandle in scope.GetLocalVariables())
            {
                var local = pdbReader.GetLocalVariable(localHandle);
                var localName = pdbReader.GetString(local.Name);
                var localValue = ilFrame.GetLocalVariable(local.Index);
                result.Add(localName, localValue.ToBrowsableObject());
            }
        }

        return result.ToImmutable();
    }
}
