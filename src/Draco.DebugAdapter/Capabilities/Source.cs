using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Model;
using Draco.Debugger;

namespace Draco.DebugAdapter;

internal sealed partial class DracoDebugAdapter
{
    private readonly Dictionary<int, (Source Source, SourceFile SourceFile)> sources = new();

    public Task<SourceResponse> GetSourceAsync(SourceArguments args)
    {
        return Task.FromResult(new SourceResponse()
        {
            Content = this.sources.TryGetValue(args.SourceReference, out var sourcePair)
                ? sourcePair.SourceFile.Text
                : string.Empty,
        });
    }

    private SourceRange? TranslateSourceRange(SourceRange? range)
    {
        int TranslateLine(int line) => line + ((this.clientInfo.LinesStartAt1 ?? false) ? 1 : 0);
        int TranslateColumn(int col) => col + ((this.clientInfo.ColumnsStartAt1 ?? false) ? 1 : 0);

        if (range is null) return null;
        var r = range.Value;
        return new()
        {
            StartLine = TranslateLine(r.StartLine),
            StartColumn = TranslateColumn(r.StartColumn),
            EndLine = TranslateLine(r.EndLine),
            EndColumn = TranslateColumn(r.EndColumn),
        };
    }

    private Source? TranslateSource(SourceFile? sourceFile) => sourceFile is null
        ? null
        : new()
        {
            Path = sourceFile.Uri.AbsolutePath,
            Name = Path.GetFileName(sourceFile.Uri.AbsolutePath),
        };
}
