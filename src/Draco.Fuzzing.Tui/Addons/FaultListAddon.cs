using Draco.Fuzzing.Tracing;
using System.Collections.Generic;
using System;
using Terminal.Gui;
using System.Linq;

namespace Draco.Fuzzing.Tui.Addons;

/// <summary>
/// An addon for displaying the faults that happened.
/// </summary>
/// <typeparam name="TInput">The type of the input model.</typeparam>
public sealed class FaultListAddon<TInput> : FuzzerAddon
{
    private sealed class Item(InputWithId<TInput> inputWithId, FaultResult fault)
    {
        public TInput Input => inputWithId.Input;
        public FaultResult Fault => fault;

        private readonly string label = FaultResultToString(fault);

        public override string ToString() => this.label;
    }

    /// <summary>
    /// Utility to format fault results into a compact message.
    /// </summary>
    /// <param name="fault">The fault result.</param>
    /// <returns>The formatted message.</returns>
    public static string FaultResultToString(FaultResult fault)
    {
        if (fault.ThrownException is not null)
        {
            return $"{fault.ThrownException.GetType().Name}: {fault.ThrownException.Message}";
        }
        if (fault.TimeoutReached is not null) return "Timeout";
        if (fault.ExitCode != 0) return $"Exit code: {fault.ExitCode}";
        return "Unknown";
    }

    /// <summary>
    /// A function to convert an input to a string visualization.
    /// </summary>
    public Func<TInput, string>? InputToString { get; set; }

    /// <summary>
    /// The faults that happened.
    /// </summary>
    public IEnumerable<KeyValuePair<TInput, FaultResult>> Faults =>
        this.items.Select(item => new KeyValuePair<TInput, FaultResult>(item.Input, item.Fault));

    // State
    private readonly List<Item> items = [];

    // UI
    private readonly FrameView faultsFrameView;
    private readonly ListView faultsListView;
    private readonly TextView selectedFaultTextView;

    public FaultListAddon()
    {
        this.faultsListView = new(this.items)
        {
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
        };
        this.selectedFaultTextView = new()
        {
            ReadOnly = true,
            X = Pos.Right(this.faultsListView),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        this.faultsListView.SelectedItemChanged += e =>
        {
            var selectedItem = e.Value as Item;
            var inputToString = this.InputToString ?? InputToStringDefault;
            this.selectedFaultTextView.Text = selectedItem is null
                ? inputToString(default!)
                : inputToString(selectedItem.Input);
        };
        this.faultsFrameView = new();
        this.faultsFrameView.Add(this.faultsListView, this.selectedFaultTextView);
        this.UpdateFrameTitle();
    }

    public override void Register(IFuzzerApplication application)
    {
        base.Register(application);
        application.Tracer.OnInputFaulted += (sender, args) =>
        {
            var unErasedInput = UnErase(args.Input);
            var item = new Item(unErasedInput, args.Fault);
            this.items.Add(item);
            this.UpdateFrameTitle();
        };
    }

    public override View CreateView() => this.faultsFrameView;

    public override MenuBarItem CreateMenuBarItem() =>
        new("Faults", [new MenuItem("Clear", "Clears the fault list", this.Clear)]);

    private void Clear()
    {
        this.items.Clear();
        this.UpdateFrameTitle();
    }

    private void UpdateFrameTitle() =>
        this.faultsFrameView.Title = $"Faults ({this.items.Count})";

    private static InputWithId<TInput> UnErase(InputWithId<object?> inputWithId) =>
        new(inputWithId.Id, (TInput)inputWithId.Input!);

    private static string InputToStringDefault(TInput input) => input?.ToString() ?? string.Empty;
}
