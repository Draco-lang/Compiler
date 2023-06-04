using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// Represents a source file from the debugged process.
/// </summary>
public sealed class SourceFile
{
    /// <summary>
    /// The session cache.
    /// </summary>
    internal SessionCache SessionCache => this.Module.SessionCache;

    /// <summary>
    /// The document handle of this source file.
    /// </summary>
    internal DocumentHandle DocumentHandle { get; }

    /// <summary>
    /// The module this source file belongs to.
    /// </summary>
    public Module Module { get; }

    /// <summary>
    /// The path of the source file.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// The text of the source file.
    /// </summary>
    public string Text => this.text ??= this.BuildText();
    private string? text;

    /// <summary>
    /// The lines of the source file.
    /// </summary>
    public ImmutableArray<ReadOnlyMemory<char>> Lines => this.lines ??= this.BuildLines();
    private ImmutableArray<ReadOnlyMemory<char>>? lines;

    public ImmutableArray<Method> Methods => this.methods ??= this.BuildMethods();
    private ImmutableArray<Method>? methods;

    internal SourceFile(Module module, DocumentHandle documentHandle, Uri uri)
    {
        this.Module = module;
        this.DocumentHandle = documentHandle;
        this.Uri = uri;
    }

    /// <summary>
    /// Attempts to place a breakpoint in this source file.
    /// </summary>
    /// <param name="position">The position to place the breakpoint at.</param>
    /// <param name="breakpoint">The placed breakpoint, if any.</param>
    /// <returns>True, if the breakpoint was successfully placed, false otherwise.</returns>
    public bool TryPlaceBreakpoint(SourcePosition position, [MaybeNullWhen(false)] out Breakpoint breakpoint)
    {
        foreach (var m in this.Methods)
        {
            if (m.TryPlaceBreakpoint(position, out breakpoint)) return true;
        }

        breakpoint = null;
        return false;
    }

    /// <summary>
    /// Attempts to place a breakpoint in this source file.
    /// </summary>
    /// <param name="line">The line to place the breakpoint at.</param>
    /// <param name="breakpoint">The placed breakpoint, if any.</param>
    /// <returns>True, if the breakpoint was successfully placed, false otherwise.</returns>
    public bool TryPlaceBreakpoint(int line, [MaybeNullWhen(false)] out Breakpoint breakpoint)
    {
        foreach (var m in this.Methods)
        {
            if (m.TryPlaceBreakpoint(line, out breakpoint)) return true;
        }

        breakpoint = null;
        return false;
    }

    private string BuildText() => File.ReadAllText(this.Uri.LocalPath);

    private ImmutableArray<Method> BuildMethods()
    {
        var meta = this.Module.CorDebugModule.GetMetaDataInterface();
        var types = meta.MetaDataImport.EnumTypeDefs();
        // NOTE: Not the most efficient
        return types
            .SelectMany(meta.MetaDataImport.EnumMethods)
            .Select(this.Module.CorDebugModule.GetFunctionFromToken)
            .Select(this.SessionCache.GetMethod)
            .Where(m => m.DebugInfo.Document == this.DocumentHandle)
            .ToImmutableArray();
    }

    private ImmutableArray<ReadOnlyMemory<char>> BuildLines()
    {
        var text = this.Text.AsMemory();
        var textSpan = text.Span;
        var result = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();

        var prevLineStart = 0;
        for (var i = 0; i < textSpan.Length;)
        {
            var newlineLength = NewlineLength(textSpan, i);
            if (newlineLength > 0)
            {
                // This is a newline
                i += newlineLength;
                result.Add(text[prevLineStart..i]);
                prevLineStart = i;
            }
            else
            {
                // Regular character
                ++i;
            }
        }

        // Add last line
        result.Add(text[prevLineStart..textSpan.Length]);

        return result.ToImmutable();
    }

    private static int NewlineLength(ReadOnlySpan<char> str, int offset)
    {
        if (offset < 0 || offset >= str.Length) return 0;
        if (str[offset] == '\r')
        {
            // Windows or OS-X 9
            if (offset + 1 < str.Length && str[offset + 1] == '\n') return 2;
            else return 1;
        }
        if (str[offset] == '\n') return 1;
        return 0;
    }
}
