using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DebuggerApi = Draco.Debugger;
using DapModels = Draco.Dap.Model;
using System.IO;
using System.Runtime.CompilerServices;

namespace Draco.DebugAdapter;

/// <summary>
/// Translation between DAP and the Draco Debugger API.
/// </summary>
internal sealed class Translator
{
    private readonly DapModels.InitializeRequestArguments clientInfo;

    public Translator(DapModels.InitializeRequestArguments clientInfo)
    {
        this.clientInfo = clientInfo;
    }

    public DapModels.StackFrame ToDap(DebuggerApi.StackFrame frame) => new()
    {
        Id = RuntimeHelpers.GetHashCode(frame),
        // TODO
        Column = 0,
        // TODO
        Line = 0,
        Name = frame.Method.Name,
        Source = frame.Method.SourceFile is null
            ? null
            : this.ToDap(frame.Method.SourceFile),
    };

    public DebuggerApi.SourcePosition ToDebugger(int line, int column) =>
        new(Line: this.LineToDebugger(line), Column: this.ColumnToDebugger(column));

    public DapModels.Source ToDap(DebuggerApi.SourceFile sourceFile) => new()
    {
        Path = sourceFile.Uri.AbsolutePath,
        Name = Path.GetFileName(sourceFile.Uri.AbsolutePath),
    };

    public (int Line, int Column) ToDap(DebuggerApi.SourcePosition position) =>
        (Line: this.LineToDap(position.Line), Column: this.ColumnToDap(position.Column));

    public (int StartLine, int StartColumn, int EndLine, int EndColumn) ToDap(DebuggerApi.SourceRange range)
    {
        var (sl, sc) = this.ToDap(range.Start);
        var (el, ec) = this.ToDap(range.End);
        return (StartLine: sl, StartColumn: sc, EndLine: el, EndColumn: ec);
    }

    private int LineToDap(int line) => line + ((this.clientInfo.LinesStartAt1 ?? false) ? 1 : 0);
    private int ColumnToDap(int col) => col + ((this.clientInfo.ColumnsStartAt1 ?? false) ? 1 : 0);

    private int LineToDebugger(int line) => line - ((this.clientInfo.LinesStartAt1 ?? false) ? 1 : 0);
    private int ColumnToDebugger(int col) => col - ((this.clientInfo.ColumnsStartAt1 ?? false) ? 1 : 0);
}
