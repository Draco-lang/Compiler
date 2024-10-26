using System;
using Terminal.Gui;

namespace Draco.Fuzzing.Tui.Addons;

/// <summary>
/// A base class for fuzzer addons.
/// </summary>
public abstract class FuzzerAddon : IFuzzerAddon
{
    /// <summary>
    /// The fuzzer visualizing application.
    /// </summary>
    public IFuzzerApplication Application =>
        this.application ?? throw new InvalidOperationException("addon not registered or base.Register not called");
    private IFuzzerApplication? application;

    /// <summary>
    /// The fuzzer model.
    /// </summary>
    public IFuzzer Fuzzer => this.Application.Fuzzer;

    public virtual string Name
    {
        get
        {
            var typeName = this.GetType().Name;
            var backtickIndex = typeName.IndexOf('`');
            if (backtickIndex >= 0) typeName = typeName[..backtickIndex];
            if (typeName.EndsWith("Addon", StringComparison.Ordinal)) typeName = typeName[..^5];
            return typeName;
        }
    }

    public virtual void Register(IFuzzerApplication application)
    {
        this.application = application;
    }

    public virtual MenuBarItem? CreateMenuBarItem() => null;
    public virtual StatusItem? CreateStatusItem() => null;
    public virtual View? CreateView() => null;
}
