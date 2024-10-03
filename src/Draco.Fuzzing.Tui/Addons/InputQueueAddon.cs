using System;
using System.Collections.Generic;
using Draco.Fuzzing.Tracing;
using Terminal.Gui;

namespace Draco.Fuzzing.Tui.Addons;

/// <summary>
/// An addon for displaying the input queue.
/// </summary>
/// <typeparam name="TInput">The type of the input model.</typeparam>
public sealed class InputQueueAddon<TInput> : FuzzerAddon
{
    private sealed class Item(InputWithId<TInput> inputWithId, string label)
    {
        public TInput Input => inputWithId.Input;
        public int Id => inputWithId.Id;

        public override string ToString() => label;
    }

    /// <summary>
    /// The maximum number of items to visualize in the queue.
    /// -1 means no limit.
    /// </summary>
    public int MaxVisualizedItems { get; set; } = -1;

    /// <summary>
    /// A function to get the label of an input.
    /// </summary>
    public Func<InputWithId<TInput>, string>? GetLabel { get; set; }

    /// <summary>
    /// A function to convert an input to a string visualization.
    /// </summary>
    public Func<TInput, string>? InputToString { get; set; }

    private bool HasSpaceInVisualizedItems => this.MaxVisualizedItems == -1 || this.items.Count < this.MaxVisualizedItems;

    // State
    private readonly List<Item> items = [];
    private readonly Queue<Item> nonVisualizedItems = [];

    // UI
    private readonly FrameView inputsFrameView;
    private readonly ListView inputsListView;
    private readonly TextView selectedInputTextView;

    public InputQueueAddon()
    {
        this.inputsListView = new(this.items)
        {
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
        };
        this.selectedInputTextView = new()
        {
            ReadOnly = true,
            X = Pos.Right(this.inputsListView),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        this.inputsListView.SelectedItemChanged += e =>
        {
            var inputToString = this.InputToString ?? InputToStringDefault;
            this.selectedInputTextView.Text = e.Value is not Item selectedItem
                ? inputToString(default!)
                : inputToString(selectedItem.Input);
        };
        this.inputsFrameView = new();
        this.inputsFrameView.Add(this.inputsListView, this.selectedInputTextView);
        this.UpdateFrameTitle();
    }

    public override void Register(IFuzzerApplication application)
    {
        base.Register(application);
        application.Tracer.OnInputsEnqueued += (sender, args) =>
        {
            var getLabel = this.GetLabel ?? GetLabelDefault;
            foreach (var input in args.Inputs)
            {
                var unErasedInput = UnErase(input);
                var item = new Item(unErasedInput, getLabel(unErasedInput));
                this.AddItem(item);
            }
            this.UpdateFrameTitle();
        };
        application.Tracer.OnInputDequeued += (sender, args) =>
        {
            if (!this.RemoveItem(args.Input.Id)) return;
            this.MoveItemsToVisualized();
            this.UpdateFrameTitle();
        };
    }

    public override View CreateView() => this.inputsFrameView;

    private void AddItem(Item item)
    {
        if (this.HasSpaceInVisualizedItems)
        {
            this.items.Add(item);
        }
        else
        {
            this.nonVisualizedItems.Enqueue(item);
        }
    }

    private bool RemoveItem(int id)
    {
        var itemIndex = this.items.FindIndex(item => item.Id == id);
        if (itemIndex != -1)
        {
            this.items.RemoveAt(itemIndex);
            return true;
        }
        // NOTE: This can cause a bug, if the parallelism is bigger than the visualized list count
        // as we forget to remove from the non-visualized queue...
        return false;
    }

    private void MoveItemsToVisualized()
    {
        while (this.HasSpaceInVisualizedItems && this.nonVisualizedItems.TryDequeue(out var item))
        {
            this.items.Add(item);
        }
    }

    private void UpdateFrameTitle() =>
        this.inputsFrameView.Title = $"Inputs ({this.items.Count + this.nonVisualizedItems.Count})";

    private static InputWithId<TInput> UnErase(InputWithId<object?> inputWithId) =>
        new(inputWithId.Id, (TInput)inputWithId.Input!);

    private static string GetLabelDefault(InputWithId<TInput> input) => $"Input {input.Id}";

    private static string InputToStringDefault(TInput input) => input?.ToString() ?? string.Empty;
}
