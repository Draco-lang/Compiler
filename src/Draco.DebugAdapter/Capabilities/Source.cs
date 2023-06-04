using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    private Source? TranslateSource(SourceFile? sourceFile)
    {
        if (sourceFile is null) return null;
        var sourceReference = RuntimeHelpers.GetHashCode(sourceFile);
        if (!this.sources.TryGetValue(sourceReference, out var sourcePair))
        {
            var source = new Source()
            {
                // TODO: Incorrect
                Path = System.IO.Path.GetFileName(sourceFile.Uri.LocalPath),
                SourceReference = sourceReference,
            };
            this.sources.Add(sourceReference, (source, sourceFile));
        }
        return sourcePair.Source;
    }
}
