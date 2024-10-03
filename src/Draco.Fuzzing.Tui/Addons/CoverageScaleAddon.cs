using System;
using System.Linq;
using Draco.Coverage;
using Draco.Fuzzing.Tracing;
using Terminal.Gui;

namespace Draco.Fuzzing.Tui.Addons;

/// <summary>
/// An addon for showing the current and best coverages as a scale/bar.
/// </summary>
public sealed class CoverageScaleAddon : FuzzerAddon
{
    // State
    private double bestCoveragePercent = 0;

    // UI
    private readonly FrameView coverageFrameView;
    private readonly Label currentCoverageLabel;
    private readonly Label bestCoverageLabel;
    private readonly ProgressBar currentCoverageProgressBar;
    private readonly ProgressBar bestCoverageProgressBar;
    private readonly Label currentCoveragePercentageLabel;
    private readonly Label bestCoveragePercentageLabel;

    public CoverageScaleAddon()
    {
        this.currentCoverageLabel = new("Current:");
        this.bestCoverageLabel = new("Best:   ")
        {
            Y = 2,
        };
        this.currentCoverageProgressBar = new()
        {
            X = Pos.Right(this.currentCoverageLabel) + 1,
            Y = 0,
            Width = Dim.Fill(5),
            Height = 1,
            ProgressBarStyle = ProgressBarStyle.Continuous,
        };
        this.bestCoverageProgressBar = new()
        {
            X = Pos.Right(this.bestCoverageLabel) + 1,
            Y = 2,
            Width = Dim.Fill(5),
            Height = 1,
            ProgressBarStyle = ProgressBarStyle.Continuous,
        };
        this.currentCoveragePercentageLabel = new("  0%")
        {
            X = Pos.Right(this.currentCoverageProgressBar) + 1,
        };
        this.bestCoveragePercentageLabel = new("  0%")
        {
            X = Pos.Right(this.bestCoverageProgressBar) + 1,
            Y = 2,
        };
        this.coverageFrameView = new FrameView("Coverage")
        {
            Height = Dim.Sized(5),
            CanFocus = false,
        };
        this.coverageFrameView.Add(
            this.currentCoverageLabel,
            this.bestCoverageLabel,
            this.currentCoverageProgressBar,
            this.bestCoverageProgressBar,
            this.currentCoveragePercentageLabel,
            this.bestCoveragePercentageLabel);
    }

    public override void Register(IFuzzerApplication application)
    {
        base.Register(application);
        application.Tracer.OnInputFuzzEnded += this.OnInputFuzzEnded;
    }

    public override View? CreateView() => this.coverageFrameView;

    private void OnInputFuzzEnded(object? sender, InputFuzzEndedEventArgs<object?> args)
    {
        var currentCoveragePercent = CoverageToPercentage(args.CoverageResult);
        this.bestCoveragePercent = Math.Max(this.bestCoveragePercent, currentCoveragePercent);

        this.currentCoverageProgressBar.Fraction = (float)currentCoveragePercent;
        this.currentCoveragePercentageLabel.Text = FormatPercentage(currentCoveragePercent);

        this.bestCoverageProgressBar.Fraction = (float)this.bestCoveragePercent;
        this.bestCoveragePercentageLabel.Text = FormatPercentage(this.bestCoveragePercent);
    }

    private static double CoverageToPercentage(CoverageResult coverage) =>
        coverage.Hits.Count(h => h > 0) / (double)coverage.Hits.Length;

    private static string FormatPercentage(double percentage) =>
        $"{(int)(percentage * 100),3}%";
}
