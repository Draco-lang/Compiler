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
    /// A function to get the label of an input.
    /// </summary>
    public Func<InputWithId<TInput>, string>? GetLabel { get; set; }

    // State
    private readonly List<Item> items = [];

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
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        this.inputsFrameView = new();
        this.inputsFrameView.Add(this.inputsListView, this.selectedInputTextView);
        this.UpdateFrameTitle();
    }

    public override void Register(IFuzzerApplication application)
    {
        application.Tracer.OnInputsEnqueued += (sender, args) =>
        {
            var getLabel = this.GetLabel ?? (inputWithId => $"Input {inputWithId.Id}");
            foreach (var input in args.Inputs)
            {
                var unErasedInput = UnErase(input);
                var item = new Item(unErasedInput, getLabel(unErasedInput));
                this.items.Add(item);
            }
            this.UpdateFrameTitle();
        };
        application.Tracer.OnInputDequeued += (sender, args) =>
        {
            var itemIndex = this.items.FindIndex(item => item.Id == args.Input.Id);
            if (itemIndex == -1) return;
            this.items.RemoveAt(itemIndex);
            this.UpdateFrameTitle();
        };
    }

    public override View CreateView() => this.inputsFrameView;

    private void UpdateFrameTitle() => this.inputsFrameView.Title = $"Inputs ({this.items.Count})";

    private static InputWithId<TInput> UnErase(InputWithId<object?> inputWithId) =>
        new(inputWithId.Id, (TInput)inputWithId.Input!);
}
