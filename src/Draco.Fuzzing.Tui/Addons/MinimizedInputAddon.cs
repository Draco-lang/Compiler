using System;
using System.Collections.Generic;
using Draco.Fuzzing.Tracing;
using Terminal.Gui;

namespace Draco.Fuzzing.Tui.Addons;

/// <summary>
/// An addon to display the minimized input.
/// </summary>
/// <typeparam name="TInput">The type of the input model.</typeparam>
public sealed class MinimizedInputAddon<TInput> : FuzzerAddon
{
    /// <summary>
    /// A function to convert an input to a string visualization.
    /// </summary>
    public Func<TInput, string>? InputToString { get; set; }

    // State
    private readonly Dictionary<int, int> inputMinimizationCounts = [];

    // UI
    private readonly FrameView minimizedInputFrameView;
    private readonly TextView minimizedInputTextView;

    public MinimizedInputAddon()
    {
        this.minimizedInputTextView = new()
        {
            ReadOnly = true,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        this.minimizedInputFrameView = new FrameView("Minimized Input");
        this.minimizedInputFrameView.Add(this.minimizedInputTextView);
    }

    public override void Register(IFuzzerApplication application)
    {
        base.Register(application);

        application.Tracer.OnInputDequeued += this.OnInputDequeued;
        application.Tracer.OnInputDropped += this.OnInputDropped;
        application.Tracer.OnMinimizationFound += this.OnMinimizationFound;
    }

    public override View CreateView() => this.minimizedInputFrameView;

    private void OnInputDequeued(object? sender, InputDequeuedEventArgs<object?> e)
    {
        this.minimizedInputFrameView.Title = "Minimized Input";
        this.inputMinimizationCounts.Add(e.Input.Id, 0);

        this.minimizedInputTextView.Text = string.Empty;
    }

    private void OnInputDropped(object? sender, InputDroppedEventArgs<object?> e) =>
        this.inputMinimizationCounts.Remove(e.Input.Id);

    private void OnMinimizationFound(object? sender, MinimizationFoundEventArgs<object?> e)
    {
        if (!this.inputMinimizationCounts.TryGetValue(e.Input.Id, out var minimizations))
        {
            minimizations = 0;
        }
        ++minimizations;
        this.inputMinimizationCounts[e.Input.Id] = minimizations;
        this.minimizedInputFrameView.Title = $"Minimized Input (Minimizations: {minimizations})";

        var inputToString = this.InputToString ?? InputToStringDefault;
        var unErasedInput = UnErase(e.MinimizedInput);
        this.minimizedInputTextView.Text = inputToString(unErasedInput.Input);
    }

    private static InputWithId<TInput> UnErase(InputWithId<object?> inputWithId) =>
        new(inputWithId.Id, (TInput)inputWithId.Input!);

    private static string InputToStringDefault(TInput input) => input?.ToString() ?? string.Empty;
}
