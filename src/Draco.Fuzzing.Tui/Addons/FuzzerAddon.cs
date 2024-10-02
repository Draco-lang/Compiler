using Draco.Fuzzing.Tracing;
using Terminal.Gui;

namespace Draco.Fuzzing.Tui.Addons;

/// <summary>
/// A base class for fuzzer addons.
/// </summary>
public abstract class FuzzerAddon : IFuzzerAddon
{
    public abstract string Name { get; }

    public virtual void Register(IFuzzerApplication application, EventTracer<object?> tracer) { }
    public virtual MenuBarItem? CreateMenuBarItem() => null;
    public virtual StatusItem? CreateStatusItem() => null;
    public virtual View? CreateView() => null;
}
