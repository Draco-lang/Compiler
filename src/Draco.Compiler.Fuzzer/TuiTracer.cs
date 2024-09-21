using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Coverage;
using Draco.Fuzzing;
using Terminal.Gui;

namespace Draco.Compiler.Fuzzer;

internal sealed class TuiTracer : Window, ITracer<SyntaxTree>
{
    private sealed class InputQueueItem(SyntaxTree input, int index)
    {
        public SyntaxTree Input { get; } = input;
        public int Index { get; } = index;

        public override string ToString() => $"Input {this.Index}";
    }

    private sealed class FaultItem(SyntaxTree input, FaultResult fault)
    {
        public SyntaxTree Input { get; } = input;
        public FaultResult Fault { get; } = fault;

        public override string ToString()
        {
            if (this.Fault.ThrownException is not null)
            {
                return $"{this.Fault.ThrownException.GetType().Name}: {this.Fault.ThrownException.Message}";
            }
            if (this.Fault.TimeoutReached is not null) return "Timeout";
            return "Unknown";
        }
    }

    // Coverage info
    private readonly ProgressBar currentCoverageProgressBar;
    private readonly Label currentCoveragePercentLabel;
    private readonly ProgressBar bestCoverageProgressBar;
    private readonly Label bestCoveragePercentLabel;

    private double bestCoveragePercent = 0;

    // Timings
    private readonly Label fuzzesPerSecondLabel;
    private readonly Label averageTimePerFuzzLabel;
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();

    // Current input
    private readonly TextView currentInputTextView;
    private readonly TextView minimizedInputTextView;
    private readonly FrameView inputFrameView;

    private int minimizedInputCounter = 0;

    // Input queue
    private readonly ListView inputQueueListView;
    private readonly List<InputQueueItem> inputQueueList = [];
    private readonly TextView selectedInputQueueItemTextView;
    private readonly FrameView inputQueueFrameView;
    private int inputQueueItemCounter = 0;

    // Faults
    private readonly ListView faultListView;
    private readonly List<FaultItem> faultList = [];
    private readonly TextView selectedFaultItemTextView;

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
        var coverageFrameView = new FrameView("Coverage")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(70),
            Height = Dim.Sized(5),
        };
        coverageFrameView.Add(
            currentCoverageLabel, this.currentCoverageProgressBar, this.currentCoveragePercentLabel,
            bestCoverageLabel, this.bestCoverageProgressBar, this.bestCoveragePercentLabel);

        // Timings
        this.fuzzesPerSecondLabel = new Label("Fuzz/s: 0")
        {
            X = 0,
            Y = 0,
        };
        this.averageTimePerFuzzLabel = new Label("Avg/fuzz: 0ms")
        {
            X = 0,
            Y = 2,
        };
        var timingsFrameView = new FrameView("Timings")
        {
            X = Pos.Right(coverageFrameView),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Height(coverageFrameView),
        };
        timingsFrameView.Add(this.fuzzesPerSecondLabel, this.averageTimePerFuzzLabel);

        // Current input
        this.currentInputTextView = new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
        };
        var currentInputFrameView = new FrameView("Current")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
        };
        currentInputFrameView.Add(this.currentInputTextView);

        this.minimizedInputTextView = new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
        };
        var minimizedInputFrameView = new FrameView("Minimized")
        {
            X = Pos.Right(currentInputFrameView),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        minimizedInputFrameView.Add(this.minimizedInputTextView);

        this.inputFrameView = new FrameView("Input")
        {
            X = 0,
            Y = Pos.Bottom(coverageFrameView),
            Width = Dim.Fill(),
            Height = Dim.Percent(50),
        };
        this.inputFrameView.Add(currentInputFrameView, minimizedInputFrameView);

        // Input queue
        this.inputQueueListView = new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
        };
        this.inputQueueListView.SetSource(this.inputQueueList);
        this.selectedInputQueueItemTextView = new()
        {
            X = Pos.Right(this.inputQueueListView),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
        };
        this.inputQueueListView.SelectedItemChanged += e =>
        {
            var selectedItem = e.Value as InputQueueItem;
            this.selectedInputQueueItemTextView.Text = selectedItem?.Input.ToString();
        };
        this.inputQueueFrameView = new FrameView("Input Queue")
        {
            X = 0,
            Y = Pos.Bottom(this.inputFrameView),
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
        };
        this.inputQueueFrameView.Add(this.inputQueueListView, this.selectedInputQueueItemTextView);

        // Faults
        this.faultListView = new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
        };
        this.faultListView.SetSource(this.faultList);
        this.selectedFaultItemTextView = new()
        {
            X = Pos.Right(this.faultListView),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
        };
        this.faultListView.SelectedItemChanged += e =>
        {
            var selectedItem = e.Value as FaultItem;
            this.selectedFaultItemTextView.Text = selectedItem?.Input.ToString();
        };
        var faultFrameView = new FrameView("Faults")
        {
            X = Pos.Right(this.inputQueueFrameView),
            Y = Pos.Bottom(this.inputFrameView),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        faultFrameView.Add(this.faultListView, this.selectedFaultItemTextView);

        this.Add(
            coverageFrameView, timingsFrameView,
            this.inputFrameView,
            this.inputQueueFrameView, faultFrameView);

        Application.Top.Add(this);
    }

    public void InputsEnqueued(IEnumerable<SyntaxTree> inputs, IReadOnlyCollection<SyntaxTree> inputQueue)
    {
        foreach (var item in inputs) this.inputQueueList.Add(new(item, this.inputQueueItemCounter++));

        this.inputQueueFrameView.Title = $"Input Queue (Size: {this.inputQueueList.Count})";
    }

    public void InputDequeued(SyntaxTree input, IReadOnlyCollection<SyntaxTree> inputQueue)
    {
        var itemIndex = this.inputQueueList.FindIndex(item => item.Input == input);
        if (itemIndex >= 0) this.inputQueueList.RemoveAt(itemIndex);
    }

    public void EndOfMinimization(SyntaxTree input, SyntaxTree minimizedInput, CoverageResult coverage)
    {
        var currentCoveragePercent = CoverageToPercentage(coverage);
        this.bestCoveragePercent = Math.Max(this.bestCoveragePercent, currentCoveragePercent);

        this.currentCoverageProgressBar.Fraction = (float)currentCoveragePercent;
        this.currentCoveragePercentLabel.Text = FormatPercentage(currentCoveragePercent);

        this.bestCoverageProgressBar.Fraction = (float)this.bestCoveragePercent;
        this.bestCoveragePercentLabel.Text = FormatPercentage(this.bestCoveragePercent);

        this.currentInputTextView.Text = input.ToString();
        this.minimizedInputTextView.Text = minimizedInput.ToString();

        ++this.minimizedInputCounter;
        this.inputFrameView.Title = $"Input (Tested: {this.minimizedInputCounter})";

        var totalElapsed = this.stopwatch.Elapsed;
        var fuzzesPerSecond = this.minimizedInputCounter / totalElapsed.TotalSeconds;
        var averageMillisecondsPerFuzz = totalElapsed.TotalMilliseconds / this.minimizedInputCounter;
        this.fuzzesPerSecondLabel.Text = $"Fuzz/s: {fuzzesPerSecond:0.00}";
        this.averageTimePerFuzzLabel.Text = $"Avg/fuzz: {averageMillisecondsPerFuzz:0.00}ms";
    }

    public void EndOfMutations(SyntaxTree input, int mutationsFound)
    {
    }

    public void InputFaulted(SyntaxTree input, FaultResult fault)
    {
        this.faultList.Add(new(input, fault));
    }

    private static double CoverageToPercentage(CoverageResult coverage) =>
        coverage.Hits.Count(h => h > 0) / (double)coverage.Hits.Length;

    private static string FormatPercentage(double percentage) =>
        $"{(int)(percentage * 100),3}%";
}
