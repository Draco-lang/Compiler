using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Coverage;
using Draco.Fuzzing;
using Draco.Fuzzing.Tracing;
using Terminal.Gui;

namespace Draco.Compiler.Fuzzer;

internal sealed class TuiTracer : Window, ITracer<SyntaxTree>
{
    private sealed class InputQueueItem(SyntaxTree input, int index)
    {
        public SyntaxTree Input { get; } = input;
        public int Index { get; } = index;

        private readonly string labelString = $"Input {index}";

        public override string ToString() => this.labelString;
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
            if (this.Fault.ExitCode != 0) return $"Exit code: {this.Fault.ExitCode}";
            return "Unknown";
        }
    }

    public Fuzzer<SyntaxTree, string, int>? Fuzzer { get; private set; }

    // Coverage info
    private readonly ProgressBar currentCoverageProgressBar;
    private readonly Label currentCoveragePercentLabel;
    private readonly ProgressBar bestCoverageProgressBar;
    private readonly Label bestCoveragePercentLabel;

    // Timings
    private readonly Label fuzzesPerSecondLabel;
    private readonly Label averageTimePerFuzzLabel;
    private readonly Stopwatch stopwatch = new();

    // Current input
    private readonly TextView currentInputTextView;
    private readonly TextView minimizedInputTextView;
    private readonly FrameView currentInputFrameView;
    private readonly FrameView minimizedInputFrameView;
    private readonly FrameView inputFrameView;

    // Input queue
    private readonly ListView inputQueueListView;
    private readonly List<InputQueueItem> inputQueueList = [];
    private readonly TextView selectedInputQueueItemTextView;
    private readonly FrameView inputQueueFrameView;

    // Faults
    private readonly ListView faultListView;
    private readonly List<FaultItem> faultList = [];
    private readonly TextView selectedFaultItemTextView;

    // Status bar
    private readonly StatusItem seedStatusItem;

    // Counters
    private int inputQueueItemCounter = 0;
    private int fuzzedInputCounter = 0;
    private int minimizedInputCounter = 0;
    private int mutatedInputCounter = 0;
    private double bestCoveragePercent = 0;
    private CoverageResult? bestCoverage;

    // Statistics
    // TargetInfo ID -> Elapsed from stopwatch
    private readonly Dictionary<int, TimeSpan> fuzzStarts = [];
    private readonly List<TimeSpan> fuzzTimings = [];

    public TuiTracer()
    {
        this.Border = new();

        #region Menu
        var menuBar = new MenuBar(
        [
            new MenuBarItem("_File", new[]
            {
                new MenuItem("_Quit", "Quits the application", () => Application.RequestStop()),
            }),
            new MenuBarItem("_Inputs", new[]
            {
                new MenuItem("_Import", "Imports inputs into the input queue", this.ImportInputs),
            }),
            new MenuBarItem("_Faults", new[]
            {
                new MenuItem("_Clear", "Clears the fault list", this.ClearFaultList, canExecute: () => this.faultList.Count > 0),
                new MenuItem("_Export", "Exports the fault list", this.ExportFaults, canExecute: () => this.faultList.Count > 0)
            }),
            new MenuBarItem("_Statistics", new[]
            {
                new MenuBarItem("_Histogram", "Exports a timing histogram", this.ExportTimingsHistogram, canExecute: () => this.fuzzTimings.Count > 0),
                new MenuBarItem("_Export LCOV", "Exports LCOV coverage info", this.ExportLcov, canExecute: () => this.bestCoverage is not null),
            }),
        ]);
        #endregion

        #region Coverage Progress Bars
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
        #endregion

        #region Timing Labels
        var fuzzesPerSecondTextLabel = new Label("Fuzz/s:   ")
        {
            X = 0,
            Y = 0,
        };
        this.fuzzesPerSecondLabel = new Label("-")
        {
            X = Pos.Right(fuzzesPerSecondTextLabel),
            Y = 0,
        };
        var averageTimePerFuzzTextLabel = new Label("Avg/fuzz: ")
        {
            X = 0,
            Y = 2,
        };
        this.averageTimePerFuzzLabel = new Label("-")
        {
            X = Pos.Right(averageTimePerFuzzTextLabel),
            Y = 2,
        };
        var timingsFrameView = new FrameView("Timings")
        {
            X = Pos.Right(coverageFrameView),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Height(coverageFrameView),
        };
        timingsFrameView.Add(
            fuzzesPerSecondTextLabel, this.fuzzesPerSecondLabel,
            averageTimePerFuzzTextLabel, this.averageTimePerFuzzLabel);
        #endregion

        #region Input Text Views
        this.currentInputTextView = new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
        };
        this.currentInputFrameView = new FrameView(GetCurrentInputFrameTitle())
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
        };
        this.currentInputFrameView.Add(this.currentInputTextView);

        this.minimizedInputTextView = new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
        };
        this.minimizedInputFrameView = new FrameView(GetMinimizedInputFrameTitle())
        {
            X = Pos.Right(this.currentInputFrameView),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        this.minimizedInputFrameView.Add(this.minimizedInputTextView);

        this.inputFrameView = new FrameView("Input")
        {
            X = 0,
            Y = Pos.Bottom(coverageFrameView),
            Width = Dim.Fill(),
            Height = Dim.Percent(50),
        };
        this.inputFrameView.Add(this.currentInputFrameView, this.minimizedInputFrameView);
        #endregion

        #region Input Queue
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
        this.inputQueueFrameView = new FrameView(GetInputQueueFrameTitle())
        {
            X = 0,
            Y = Pos.Bottom(this.inputFrameView),
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
        };
        this.inputQueueFrameView.Add(this.inputQueueListView, this.selectedInputQueueItemTextView);
        #endregion

        #region Fault List
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
        #endregion

        #region Status Bar
        this.seedStatusItem = new StatusItem(Key.Null, GetSeedStatusBarTitle(), () => { });

        var statusBar = new StatusBar([this.seedStatusItem]);
        #endregion

        this.Add(
            coverageFrameView, timingsFrameView,
            this.inputFrameView,
            this.inputQueueFrameView, faultFrameView);

        Application.Top.Add(menuBar, this, statusBar);
    }

    public void SetFuzzer(Fuzzer<SyntaxTree, string, int> fuzzer)
    {
        this.Fuzzer = fuzzer;
        this.seedStatusItem.Title = GetSeedStatusBarTitle(fuzzer.Seed);
    }

    public void InputsEnqueued(IEnumerable<SyntaxTree> inputs)
    {
        foreach (var item in inputs) this.inputQueueList.Add(new(item, this.inputQueueItemCounter++));

        this.inputQueueFrameView.Title = GetInputQueueFrameTitle(this.inputQueueList.Count);
    }

    public void InputDequeued(SyntaxTree input)
    {
        this.minimizedInputCounter = 0;
        this.mutatedInputCounter = 0;

        var itemIndex = this.inputQueueList.FindIndex(item => item.Input == input);
        if (itemIndex >= 0) this.inputQueueList.RemoveAt(itemIndex);

        this.currentInputFrameView.Title = GetCurrentInputFrameTitle();
        this.minimizedInputFrameView.Title = GetMinimizedInputFrameTitle();
        this.inputQueueFrameView.Title = GetInputQueueFrameTitle(this.inputQueueList.Count);
        this.currentInputTextView.Text = input.ToString();
        this.minimizedInputTextView.Text = string.Empty;
    }

    public void InputFuzzStarted(SyntaxTree input, TargetInfo targetInfo)
    {
        this.fuzzStarts.Add(targetInfo.Id, this.stopwatch.Elapsed);
    }

    public void InputFuzzEnded(SyntaxTree input, TargetInfo targetInfo, CoverageResult coverageResult)
    {
        // Counters
        ++this.fuzzedInputCounter;

        var totalElapsed = this.stopwatch.Elapsed;
        var fuzzesPerSecond = this.fuzzedInputCounter / totalElapsed.TotalSeconds;
        var averageMillisecondsPerFuzz = totalElapsed.TotalMilliseconds / this.fuzzedInputCounter;

        this.fuzzesPerSecondLabel.Text = fuzzesPerSecond.ToString("0.00");
        this.averageTimePerFuzzLabel.Text = $"{averageMillisecondsPerFuzz:0.00}ms";

        // Coverage percentage
        var currentCoveragePercent = CoverageToPercentage(coverageResult);
        if (currentCoveragePercent > this.bestCoveragePercent)
        {
            this.bestCoverage = coverageResult;
            this.bestCoveragePercent = currentCoveragePercent;
        }

        this.currentCoverageProgressBar.Fraction = (float)currentCoveragePercent;
        this.currentCoveragePercentLabel.Text = FormatPercentage(currentCoveragePercent);

        this.bestCoverageProgressBar.Fraction = (float)this.bestCoveragePercent;
        this.bestCoveragePercentLabel.Text = FormatPercentage(this.bestCoveragePercent);

        // Statistics
        if (this.fuzzStarts.Remove(targetInfo.Id, out var startTime))
        {
            var elapsed = totalElapsed - startTime;
            this.fuzzTimings.Add(elapsed);
        }
    }

    public void MinimizationFound(SyntaxTree input, SyntaxTree minimizedInput)
    {
        ++this.minimizedInputCounter;

        this.minimizedInputFrameView.Title = GetMinimizedInputFrameTitle(this.minimizedInputCounter);
        this.minimizedInputTextView.Text = minimizedInput.ToString();
    }

    public void MutationFound(SyntaxTree input, SyntaxTree mutatedInput)
    {
        ++this.mutatedInputCounter;

        this.currentInputFrameView.Title = GetCurrentInputFrameTitle(this.mutatedInputCounter);
    }

    public void InputFaulted(SyntaxTree input, FaultResult fault)
    {
        this.faultList.Add(new(input, fault));
    }

    public void FuzzerStarted()
    {
        this.stopwatch.Start();
    }

    public void FuzzerFinished() => MessageBox.ErrorQuery("Fuzzer Finished", "The fuzzer has finished.", "OK");

    private void ImportInputs()
    {
        this.CheckForFuzzer();

        var dialog = new OpenDialog("Import Inputs", "Import inputs into the input queue", [".draco"])
        {
            CanChooseDirectories = false,
            CanChooseFiles = true,
            AllowsMultipleSelection = true,
        };

        Application.Run(dialog);

        if (dialog.Canceled) return;

        var syntaxTrees = dialog.FilePaths
            .Select(path => SyntaxTree.Parse(File.ReadAllText(path)))
            .ToList();
        this.Fuzzer!.EnqueueRange(syntaxTrees);
    }

    private void ClearFaultList()
    {
        this.faultList.Clear();
        this.faultListView.SetSource(this.faultList);
    }

    private void ExportFaults()
    {
        var dialog = new SaveDialog("Export Faults", "Export the fault list", [".txt"])
        {
            CanCreateDirectories = true,
        };

        Application.Run(dialog);

        if (dialog.Canceled) return;
        if (dialog.FileName is null) return;

        var targetPath = Path.Join(dialog.DirectoryPath.ToString()!, dialog.FileName.ToString()!);
        var faults = $"""
            //////////////////////////////////////////
            // Seed: {this.Fuzzer?.Seed}
            //////////////////////////////////////////

            {string.Join(Environment.NewLine, this.faultList.Select(FormatFaultForExport))}
            """;
        File.WriteAllText(targetPath, faults);
    }

    private void ExportTimingsHistogram()
    {
        var dialog = new SaveDialog("Export Timings Histogram", "Export the timings histogram", [".csv"])
        {
            CanCreateDirectories = true,
        };

        Application.Run(dialog);

        if (dialog.Canceled) return;
        if (dialog.FileName is null) return;

        var targetPath = Path.Join(dialog.DirectoryPath.ToString()!, dialog.FileName.ToString()!);
        var timings = this.fuzzTimings
            .Select(timing => timing.TotalMilliseconds.ToString())
            .ToList();
        File.WriteAllLines(targetPath, timings);
    }

    private void ExportLcov()
    {
        var dialog = new SaveDialog("Export LCOV Coverage", "Export the best LCOV coverage", [".lcov"])
        {
            CanCreateDirectories = true,
        };

        Application.Run(dialog);

        if (dialog.Canceled) return;
        if (dialog.FileName is null) return;

        var targetPath = Path.Join(dialog.DirectoryPath.ToString()!, dialog.FileName.ToString()!);
        var lcov = this.bestCoverage!.Value.ToLcov();
        File.WriteAllText(targetPath, lcov);
    }

    private void CheckForFuzzer()
    {
        if (this.Fuzzer is not null) return;

        throw new InvalidOperationException("fuzzer is not set");
    }

    private static string GetSeedStatusBarTitle(int? seed = null) => seed is null
        ? "Seed: -"
        : $"Seed: {seed}";

    private static string GetInputQueueFrameTitle(int? count = null) => count is null
        ? "Input Queue"
        : $"Input Queue (Size: {count})";

    private static string GetCurrentInputFrameTitle(int? count = null) => count is null
        ? "Current"
        : $"Current (Mutations: {count})";

    private static string GetMinimizedInputFrameTitle(int? count = null) => count is null
        ? "Minimized"
        : $"Minimized (Minimizations: {count})";

    private static double CoverageToPercentage(CoverageResult coverage) =>
        coverage.Hits.Count(h => h > 0) / (double)coverage.Hits.Length;

    private static string FormatPercentage(double percentage) =>
        $"{(int)(percentage * 100),3}%";

    private static string FormatFaultForExport(FaultItem fault) => $"""
        // ----------------------------------------
        // {fault}
        // ----------------------------------------
        {fault.Input}
        """;
}
