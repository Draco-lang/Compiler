using System;
using System.Threading;
using Terminal.Gui;

namespace Draco.Fuzzing.Tui.Addons;

/// <summary>
/// An addon for starting and stopping the fuzzer from the menu.
/// </summary>
public sealed class StartStopAddon : FuzzerAddon
{
    private CancellationTokenSource? cts;

    public override void Register(IFuzzerApplication application)
    {
        base.Register(application);
        application.Tracer.OnFuzzerStopped += this.OnFuzzerStopped;
    }

    public override MenuBarItem? CreateMenuBarItem() => new("File", [
        new MenuItem("Start", "Start the fuzzer", this.StartFuzzer, canExecute: () => this.cts is null),
        new MenuItem("Stop", "Stop the fuzzer", this.StopFuzzer, canExecute: () => this.cts is not null)]);

    private void StartFuzzer()
    {
        if (this.cts is not null) return;

        this.cts = new CancellationTokenSource();
        ThreadPool.QueueUserWorkItem(_ => this.Fuzzer.Run(this.cts.Token));
    }

    private void StopFuzzer()
    {
        if (this.cts is null) return;

        this.cts.Cancel();
    }

    private void OnFuzzerStopped(object? sender, EventArgs args)
    {
        if (this.cts is null) return;

        this.cts.Dispose();
        this.cts = null;
    }
}
