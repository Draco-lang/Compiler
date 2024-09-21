using System;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Coverage;
using Draco.Fuzzing;
using Terminal.Gui;

namespace Draco.Compiler.Fuzzer;

internal sealed class TuiTracer : Window, ITracer<SyntaxTree>
{
    private readonly ProgressBar currentCoverageProgressBar;
    private readonly ProgressBar bestCoverageProgressBar;

    private double bestCoveragePercent = 0;

    public TuiTracer()
    {
        var currentCoverageLabel = new Label("Current:")
        {
            X = 0,
            Y = 0,
        };
        this.currentCoverageProgressBar = new()
        {
            X = Pos.Right(currentCoverageLabel) + 1,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            ProgressBarStyle = ProgressBarStyle.Continuous,
        };

        var bestCoverageLabel = new Label("Best:   ")
        {
            X = 0,
            Y = 2,
        };
        this.bestCoverageProgressBar = new()
        {
            X = Pos.Right(bestCoverageLabel) + 1,
            Y = 2,
            Width = Dim.Fill(),
            Height = 1,
            ProgressBarStyle = ProgressBarStyle.Continuous,
        };

        var frame = new FrameView("Coverage")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Sized(5),
        };
        frame.Add(
            currentCoverageLabel, this.currentCoverageProgressBar,
            bestCoverageLabel, this.bestCoverageProgressBar);

        this.Add(frame);

        Application.Top.Add(this);
    }

    public void EndOfMinimization(SyntaxTree input, SyntaxTree minimizedInput, CoverageResult coverage, TimeSpan elapsed)
    {
        var currentCoveragePercent = CoverageToPercentage(coverage);
        this.bestCoveragePercent = Math.Max(this.bestCoveragePercent, currentCoveragePercent);

        this.currentCoverageProgressBar.Fraction = (float)currentCoveragePercent;
        this.bestCoverageProgressBar.Fraction = (float)this.bestCoveragePercent;

        Application.Refresh();
    }

    public void EndOfMutations(SyntaxTree input, int mutationsFound, TimeSpan elapsed)
    {
    }

    public void InputFaulted(SyntaxTree input, FaultResult fault)
    {
    }

    private static double CoverageToPercentage(CoverageResult coverage) =>
        coverage.Entires.Count(e => e.Hits > 0) / (double)coverage.Entires.Length;
}
