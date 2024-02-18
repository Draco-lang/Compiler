using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using DapModels = Draco.Dap.Model;
using DebuggerApi = Draco.Debugger;

namespace Draco.DebugAdapter;

/// <summary>
/// Translation between DAP and the Draco Debugger API.
/// </summary>
internal sealed class Translator
{
    private readonly DapModels.InitializeRequestArguments clientInfo;

    private readonly Dictionary<DebuggerApi.StackFrame, DapModels.StackFrame> stackFrameToDap = new();
    private readonly Dictionary<int, DebuggerApi.StackFrame> stackFrameIdToDebugger = new();
    private readonly Dictionary<int, object?> valueCache = new();
    private readonly Dictionary<DapModels.SourceBreakpoint, int> breakpointIds = new();

    public Translator(DapModels.InitializeRequestArguments clientInfo)
    {
        this.clientInfo = clientInfo;
    }

    public void ClearCache()
    {
        this.stackFrameToDap.Clear();
        this.stackFrameIdToDebugger.Clear();
    }

    public int CacheValue(object? value)
    {
        var id = RuntimeHelpers.GetHashCode(value);
        this.valueCache[id] = value;
        return id;
    }

    public void Forget(DapModels.SourceBreakpoint breakpoint) => this.breakpointIds.Remove(breakpoint);

    public int AllocateId(DapModels.SourceBreakpoint breakpoint)
    {
        if (!this.breakpointIds.TryGetValue(breakpoint, out var id))
        {
            id = this.breakpointIds.Count;
            this.breakpointIds.Add(breakpoint, id);
        }
        return id;
    }

    public IList<DapModels.Variable> GetVariables(int variablesReference)
    {
        static bool IsCompound(object? value) => value is DebuggerApi.ArrayValue or DebuggerApi.ObjectValue;

        if (!this.valueCache.TryGetValue(variablesReference, out var value)) return Array.Empty<DapModels.Variable>();

        var result = new List<DapModels.Variable>();
        switch (value)
        {
        case IReadOnlyDictionary<string, object?> locals:
        {
            // Locals or object value
            foreach (var (name, val) in locals)
            {
                var id = IsCompound(val) ? this.CacheValue(val) : 0;
                result.Add(new()
                {
                    Name = name,
                    Value = val?.ToString() ?? "null",
                    VariablesReference = id,
                });
            }
            break;
        }
        case IReadOnlyList<object?> elements:
        {
            // Array
            for (var i = 0; i < elements.Count; ++i)
            {
                var val = elements[i];
                var id = IsCompound(val) ? this.CacheValue(val) : 0;
                result.Add(new()
                {
                    Name = $"[{i}]",
                    Value = val?.ToString() ?? "null",
                    VariablesReference = id,
                });
            }
            break;
        }
        }
        return result;
    }

    public DebuggerApi.StackFrame? GetStackFrameById(int id) => this.stackFrameIdToDebugger.TryGetValue(id, out var frame)
        ? frame
        : null;

    public DapModels.StackFrame ToDap(DebuggerApi.StackFrame frame)
    {
        if (!this.stackFrameToDap.TryGetValue(frame, out var existing))
        {
            var (startLine, startColumn, endLine, endColumn) = frame.Range is null
                ? (0, 0, 0, 0)
                : this.ToDap(frame.Range.Value);
            existing = new DapModels.StackFrame()
            {
                Id = this.stackFrameToDap.Count,
                Name = frame.Method.Name,
                Line = startLine,
                Column = startColumn,
                EndLine = endLine,
                EndColumn = endColumn,
                Source = frame.Method.SourceFile is null
                    ? null
                    : this.ToDap(frame.Method.SourceFile),
            };
            this.stackFrameToDap.Add(frame, existing);
            this.stackFrameIdToDebugger[existing.Id] = frame;
        }
        return existing;
    }

    public DapModels.Thread ToDap(DebuggerApi.Thread thread) => new()
    {
        Id = thread.Id,
        Name = string.IsNullOrEmpty(thread.Name)
            ? $"thread-{thread.Id}"
            : thread.Name,
    };

    public DapModels.Breakpoint ToDap(DebuggerApi.Breakpoint breakpoint, int? id)
    {
        var result = new DapModels.Breakpoint()
        {
            Verified = true,
            Id = id,
            Source = breakpoint.SourceFile is null ? null : this.ToDap(breakpoint.SourceFile),
        };
        if (breakpoint.Range is not null)
        {
            var r = breakpoint.Range.Value;
            var (startLine, startColumn, endLine, endColumn) = this.ToDap(r);
            result.Line = startLine;
            result.Column = startColumn;
            result.EndLine = endLine;
            result.EndColumn = endColumn;
        }
        return result;
    }

    public DebuggerApi.SourcePosition ToDebugger(int line, int column) =>
        new(Line: this.LineToDebugger(line), Column: this.ColumnToDebugger(column));

    public DapModels.Source ToDap(DebuggerApi.SourceFile sourceFile) => new()
    {
        Path = sourceFile.Uri.AbsolutePath,
        Name = Path.GetFileName(sourceFile.Uri.AbsolutePath),
    };

    public DapModels.Module ToDap(DebuggerApi.Module module) => new()
    {
        Id = module.Name,
        Name = module.Name,
        //Version = module.PdbReader.MetadataVersion, // not sure...
        SymbolFilePath = module.PdbName,
        Path = module.Name,
    };

    public (int Line, int Column) ToDap(DebuggerApi.SourcePosition position) =>
        (Line: this.LineToDap(position.Line), Column: this.ColumnToDap(position.Column));

    public (int StartLine, int StartColumn, int EndLine, int EndColumn) ToDap(DebuggerApi.SourceRange range)
    {
        var (sl, sc) = this.ToDap(range.Start);
        var (el, ec) = this.ToDap(range.End);
        return (StartLine: sl, StartColumn: sc, EndLine: el, EndColumn: ec);
    }

    public int LineToDap(int line) => line + ((this.clientInfo.LinesStartAt1 ?? false) ? 1 : 0);
    public int ColumnToDap(int col) => col + ((this.clientInfo.ColumnsStartAt1 ?? false) ? 1 : 0);

    public int LineToDebugger(int line) => line - ((this.clientInfo.LinesStartAt1 ?? false) ? 1 : 0);
    public int ColumnToDebugger(int col) => col - ((this.clientInfo.ColumnsStartAt1 ?? false) ? 1 : 0);
}
