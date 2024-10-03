using System;
using System.Diagnostics;
using Draco.Fuzzing.Tracing;
using Terminal.Gui;

namespace Draco.Fuzzing.Tui.Addons;

/// <summary>
/// A fuzzer addon for simple timing information.
/// </summary>
public sealed class TimingsAddon : FuzzerAddon
{
    // UI
    private readonly FrameView frameView;
    private readonly Label fuzzesPerSecondTextLabel;
    private readonly Label fuzzesPerSecondValueLabel;
    private readonly Label averageTimePerFuzzTextLabel;
    private readonly Label averageTimePerFuzzValueLabel;

    // State
    private readonly Stopwatch stopwatch = new();
    private int fuzzedCount;

    public TimingsAddon()
    {
        this.frameView = new FrameView("Timings")
        {
            Height = 5,
        };
        this.fuzzesPerSecondTextLabel = new Label("Fuzz/s:   ");
        this.fuzzesPerSecondValueLabel = new Label("-")
        {
            X = Pos.Right(this.fuzzesPerSecondTextLabel),
        };
        this.averageTimePerFuzzTextLabel = new Label("Avg/fuzz: ")
        {
            Y = 2,
        };
        this.averageTimePerFuzzValueLabel = new Label("-")
        {
            X = Pos.Right(this.averageTimePerFuzzTextLabel),
            Y = 2,
        };
        this.frameView.Add(
            this.fuzzesPerSecondTextLabel,
            this.fuzzesPerSecondValueLabel,
            this.averageTimePerFuzzTextLabel,
            this.averageTimePerFuzzValueLabel);
    }

    public override void Register(IFuzzerApplication application)
    {
        base.Register(application);

        application.Tracer.OnFuzzerStarted += this.OnFuzzerStarted;
        application.Tracer.OnFuzzerStopped += this.OnFuzzerStopped;
        application.Tracer.OnInputFuzzEnded += this.OnInputFuzzEnded;
    }

    public override View CreateView() => this.frameView;

    private void OnFuzzerStarted(object? sender, EventArgs e) => this.stopwatch.Start();

    private void OnFuzzerStopped(object? sender, EventArgs e) => this.stopwatch.Stop();

    private void OnInputFuzzEnded(object? sender, InputFuzzEndedEventArgs<object?> e)
    {
        ++this.fuzzedCount;

        var totalElapsed = this.stopwatch.Elapsed;
        var fuzzesPerSecond = this.fuzzedCount / totalElapsed.TotalSeconds;
        var averageMillisecondsPerFuzz = totalElapsed.TotalMilliseconds / this.fuzzedCount;

        this.fuzzesPerSecondValueLabel.Text = fuzzesPerSecond.ToString("0.00");
        this.averageTimePerFuzzValueLabel.Text = $"{averageMillisecondsPerFuzz:0.00}ms";
    }
}
