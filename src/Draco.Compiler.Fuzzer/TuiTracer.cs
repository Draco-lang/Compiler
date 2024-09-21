using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Coverage;
using Draco.Fuzzing;
using Terminal.Gui;

namespace Draco.Compiler.Fuzzer;

internal sealed class TuiTracer : Window, ITracer<SyntaxTree>
{
    private sealed class InputQueueItem(SyntaxTree input, int index) : IEquatable<InputQueueItem>
    {
        public SyntaxTree Input { get; } = input;
        public int Index { get; } = index;

        public override string ToString() => $"Input {this.Index}";

        public bool Equals(InputQueueItem? other) => ReferenceEquals(this.Input, other?.Input);
        public override bool Equals(object? obj) => this.Equals(obj as InputQueueItem);
        public override int GetHashCode() => this.Input.GetHashCode();
    }

    // Coverage info
    private readonly ProgressBar currentCoverageProgressBar;
    private readonly Label currentCoveragePercentLabel;
    private readonly ProgressBar bestCoverageProgressBar;
    private readonly Label bestCoveragePercentLabel;

    private double bestCoveragePercent = 0;

    // Current input
    private readonly TextView currentInputTextView;
    private readonly TextView minimizedInputTextView;

    // Input queue
    private readonly ListView inputQueueListView;
    private readonly List<InputQueueItem> inputQueueList = [];
    private readonly FrameView inputQueueFrame;
    private int inputQueueItemCounter = 0;

    public TuiTracer()
    {
        // Coverage info
        var currentCoverageLabel = new Label("Current:")
        {
            X = 0,
            Y = 0,
        };
        this.currentCoverageProgressBar = new()
        {
            X = Pos.Right(currentCoverageLabel) + 1,
            Y = 0,
            Width = Dim.Fill(5),
            Height = 1,
            ProgressBarStyle = ProgressBarStyle.Continuous,
        };
        this.currentCoveragePercentLabel = new("  0%")
        {
            X = Pos.Right(this.currentCoverageProgressBar) + 1,
            Y = 0,
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
            Width = Dim.Fill(5),
            Height = 1,
            ProgressBarStyle = ProgressBarStyle.Continuous,
        };
        this.bestCoveragePercentLabel = new("  0%")
        {
            X = Pos.Right(this.bestCoverageProgressBar) + 1,
            Y = 2,
        };
        var coverageFrame = new FrameView("Coverage")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Sized(5),
        };
        coverageFrame.Add(
            currentCoverageLabel, this.currentCoverageProgressBar, this.currentCoveragePercentLabel,
            bestCoverageLabel, this.bestCoverageProgressBar, this.bestCoveragePercentLabel);

        // Current input
        this.currentInputTextView = new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
        };
        var currentInputFrame = new FrameView("Current")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
        };
        currentInputFrame.Add(this.currentInputTextView);

        this.minimizedInputTextView = new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
        };
        var minimizedInputFrame = new FrameView("Minimized")
        {
            X = Pos.Right(currentInputFrame),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        minimizedInputFrame.Add(this.minimizedInputTextView);

        var inputFrame = new FrameView("Input")
        {
            X = 0,
            Y = Pos.Bottom(coverageFrame),
            Width = Dim.Fill(),
            Height = Dim.Percent(50),
        };
        inputFrame.Add(currentInputFrame, minimizedInputFrame);

        // Input queue
        this.inputQueueListView = new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
        };
        this.inputQueueListView.SetSource(this.inputQueueList);
        this.inputQueueFrame = new FrameView("Input Queue")
        {
            X = 0,
            Y = Pos.Bottom(inputFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        this.inputQueueFrame.Add(this.inputQueueListView);

        this.Add(
            coverageFrame,
            inputFrame,
            this.inputQueueFrame);

        Application.Top.Add(this);
    }

    public void InputsEnqueued(IEnumerable<SyntaxTree> inputs, IReadOnlyCollection<SyntaxTree> inputQueue)
    {
        foreach (var item in inputs) this.inputQueueList.Add(new(item, this.inputQueueItemCounter++));

        this.inputQueueFrame.Title = $"Input Queue (Size: {this.inputQueueList.Count})";

        Application.Refresh();
    }

    public void InputDequeued(SyntaxTree input, IReadOnlyCollection<SyntaxTree> inputQueue)
    {
        var itemIndex = this.inputQueueList.FindIndex(item => item.Input == input);
        if (itemIndex >= 0) this.inputQueueList.RemoveAt(itemIndex);

        Application.Refresh();
    }

    public void EndOfMinimization(SyntaxTree input, SyntaxTree minimizedInput, CoverageResult coverage, TimeSpan elapsed)
    {
        var currentCoveragePercent = CoverageToPercentage(coverage);
        this.bestCoveragePercent = Math.Max(this.bestCoveragePercent, currentCoveragePercent);

        this.currentCoverageProgressBar.Fraction = (float)currentCoveragePercent;
        this.currentCoveragePercentLabel.Text = FormatPercentage(currentCoveragePercent);

        this.bestCoverageProgressBar.Fraction = (float)this.bestCoveragePercent;
        this.bestCoveragePercentLabel.Text = FormatPercentage(this.bestCoveragePercent);

        this.currentInputTextView.Text = input.ToString();
        this.minimizedInputTextView.Text = minimizedInput.ToString();

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

    private static string FormatPercentage(double percentage) =>
        $"{(int)(percentage * 100),3}%";
}
