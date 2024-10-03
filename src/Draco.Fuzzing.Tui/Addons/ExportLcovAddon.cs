using System;
using System.IO;
using System.Linq;
using Draco.Coverage;
using Draco.Fuzzing.Tracing;
using Terminal.Gui;

namespace Draco.Fuzzing.Tui.Addons;

/// <summary>
/// An addon to export the best coverage found as an LCOV file.
/// </summary>
public sealed class ExportLcovAddon : FuzzerAddon
{
    private CoverageResult? bestCoverage;
    private double bestCoverageRate = -1;

    public override void Register(IFuzzerApplication application)
    {
        base.Register(application);
        application.Tracer.OnInputFuzzEnded += this.OnInputFuzzEnded;
    }

    public override MenuBarItem CreateMenuBarItem() =>
        new("Statistics", [new MenuItem("Export LCOV", "Export best coverage as LCOV", this.Export, canExecute: () => this.bestCoverage is not null)]);

    private void OnInputFuzzEnded(object? sender, InputFuzzEndedEventArgs<object?> e)
    {
        var coverage = e.CoverageResult;
        var coverageRate = CoverageRate(coverage);
        if (coverageRate > this.bestCoverageRate)
        {
            this.bestCoverage = coverage;
            this.bestCoverageRate = coverageRate;
        }
    }

    private void Export()
    {
        if (this.bestCoverage is null) return;

        var dialog = new SaveDialog("Export", "Export the best LCOV coverage", [".lcov"])
        {
            CanCreateDirectories = true,
        };

        Terminal.Gui.Application.Run(dialog);

        if (dialog.Canceled) return;
        if (dialog.FileName is null) return;

        var lcov = this.bestCoverage.Value.ToLcov();
        File.WriteAllText(dialog.FilePath.ToString()!, lcov);
    }

    private static double CoverageRate(CoverageResult coverage) =>
        coverage.Hits.Count(h => h > 0) / (double)coverage.Hits.Length;
}
