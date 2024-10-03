using System;
using System.Collections.Generic;
using Draco.Fuzzing.Tracing;
using Terminal.Gui;

namespace Draco.Fuzzing.Tui.Addons;

/// <summary>
/// An addon to display the current input.
/// </summary>
/// <typeparam name="TInput">The type of the input model.</typeparam>
public sealed class CurrentInputAddon<TInput> : FuzzerAddon
{
    /// <summary>
    /// A function to convert an input to a string visualization.
    /// </summary>
    public Func<TInput, string>? InputToString { get; set; }

    // State
    private readonly Dictionary<int, int> inputMutationCounts = [];

    // UI
    private readonly FrameView currentInputFrameView;
    private readonly TextView currentInputTextView;

    public CurrentInputAddon()
    {
        this.currentInputTextView = new()
        {
            ReadOnly = true,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        this.currentInputFrameView = new FrameView("Current Input");
        this.currentInputFrameView.Add(this.currentInputTextView);
    }

    public override void Register(IFuzzerApplication application)
    {
        base.Register(application);

        application.Tracer.OnInputDequeued += this.OnInputDequeued;
        application.Tracer.OnInputDropped += this.OnInputDropped;
        application.Tracer.OnMutationFound += this.OnMutationFound;
    }

    public override View CreateView() => this.currentInputFrameView;

    private void OnInputDequeued(object? sender, InputDequeuedEventArgs<object?> e)
    {
        this.currentInputFrameView.Title = "Current Input";
        this.inputMutationCounts.Add(e.Input.Id, 0);

        var inputToString = this.InputToString ?? InputToStringDefault;
        var unErasedInput = UnErase(e.Input);
        this.currentInputTextView.Text = inputToString(unErasedInput.Input);
    }

    private void OnInputDropped(object? sender, InputDroppedEventArgs<object?> e) =>
        this.inputMutationCounts.Remove(e.Input.Id);

    private void OnMutationFound(object? sender, MutationFoundEventArgs<object?> e)
    {
        if (!this.inputMutationCounts.TryGetValue(e.Input.Id, out var mutations))
        {
            mutations = 0;
        }
        ++mutations;
        this.inputMutationCounts[e.Input.Id] = mutations;
        this.currentInputFrameView.Title = $"Current Input (Mutations: {mutations})";

        var inputToString = this.InputToString ?? InputToStringDefault;
        var unErasedInput = UnErase(e.Input);
        this.currentInputTextView.Text = inputToString(unErasedInput.Input);
    }

    private static InputWithId<TInput> UnErase(InputWithId<object?> inputWithId) =>
        new(inputWithId.Id, (TInput)inputWithId.Input!);

    private static string InputToStringDefault(TInput input) => input?.ToString() ?? string.Empty;
}
