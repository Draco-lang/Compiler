using System.Collections.Generic;
using System;
using System.Diagnostics;
using Draco.Fuzzing.Tracing;
using System.IO;
using System.Linq;
using Terminal.Gui;

namespace Draco.Fuzzing.Tui.Addons;

/// <summary>
/// A simple addon to export fuzz times as a CSV file.
/// </summary>
public sealed class ExportFuzzTimesAddon : FuzzerAddon
{
    private readonly Stopwatch stopwatch = new();
    // TargetInfo ID -> Elapsed from stopwatch
    private readonly Dictionary<int, TimeSpan> fuzzStarts = [];
    private readonly List<TimeSpan> fuzzTimings = [];

    public override void Register(IFuzzerApplication application)
    {
        base.Register(application);
        application.Tracer.OnFuzzerStarted += this.OnFuzzerStarted;
        application.Tracer.OnInputFuzzStarted += this.OnInputFuzzStarted;
        application.Tracer.OnInputFuzzEnded += this.OnInputFuzzEnded;
    }

    public override MenuBarItem CreateMenuBarItem() =>
        new("_Statistics", [new MenuItem("Export fuzz times", "Export fuzz times as CSV", this.Export)]);

    private void OnFuzzerStarted(object? sender, EventArgs e) => this.stopwatch.Start();
    private void OnInputFuzzStarted(object? sender, InputFuzzStartedEventArgs<object?> e) =>
        this.fuzzStarts[e.TargetInfo.Id] = this.stopwatch.Elapsed;
    private void OnInputFuzzEnded(object? sender, InputFuzzEndedEventArgs<object?> e)
    {
        if (!this.fuzzStarts.Remove(e.TargetInfo.Id, out var start)) return;
        this.fuzzTimings.Add(this.stopwatch.Elapsed - start);
    }

    private void Export()
    {
        var dialog = new SaveDialog("Export", "Export fuzz timings", [".csv"])
        {
            CanCreateDirectories = true,
        };

        Terminal.Gui.Application.Run(dialog);

        if (dialog.Canceled) return;
        if (dialog.FileName is null) return;

        var timings = this.fuzzTimings
            .Select(timing => timing.TotalMilliseconds.ToString())
            .ToList();
        File.WriteAllLines(dialog.FilePath.ToString()!, timings);
    }
}
