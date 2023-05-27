using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// Represents a single frame in the call-stack.
/// </summary>
public sealed class StackFrame
{
    /// <summary>
    /// The cache for this object.
    /// </summary>
    internal SessionCache SessionCache { get; }

    /// <summary>
    /// The internal frame.
    /// </summary>
    internal CorDebugFrame CorDebugFrame { get; }

    /// <summary>
    /// The method the frame represents.
    /// </summary>
    public Method Method => this.SessionCache.GetMethod(this.CorDebugFrame.Function);

    /// <summary>
    /// The locals visible in this frame.
    /// </summary>
    public ImmutableArray<string> Locals => this.locals ??= this.BuildLocals();
    private ImmutableArray<string>? locals;

    internal StackFrame(SessionCache sessionCache, CorDebugFrame corDebugFrame)
    {
        this.SessionCache = sessionCache;
        this.CorDebugFrame = corDebugFrame;
    }

    private ImmutableArray<string> BuildLocals()
    {
        if (this.CorDebugFrame is not CorDebugILFrame ilFrame) return ImmutableArray<string>.Empty;

        var offset = ilFrame.IP.pnOffset;

        var meta = this.Method.Module.CorDebugModule.GetMetaDataInterface();
        var methodParams = meta.MetaDataImport.EnumParams(MetadataTokens.GetToken(this.Method.MethodDefinitionHandle));
        var pdbReader = this.Method.Module.PdbReader;
        var localScopes = pdbReader.GetLocalScopes(this.Method.MethodDefinitionHandle);

        var result = ImmutableArray.CreateBuilder<string>();

        // Process arguments
        foreach (var paramHandle in methodParams)
        {
            var param = meta.MetaDataImport.GetParamProps(paramHandle);
            result.Add(param.szName);
        }

        // Process locals
        // var locals = ilFrame.LocalVariables;
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
                result.Add(localName);
            }
        }

        return result.ToImmutable();
    }
}
