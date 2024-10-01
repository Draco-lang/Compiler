using System.Collections.Generic;
using System.Linq;
using Draco.Fuzzing.Tracing;
using Draco.Fuzzing.Tui.Addons;
using Terminal.Gui;

namespace Draco.Fuzzing.Tui;

/// <summary>
/// A fuzzer window that's extensible with addons.
/// </summary>
public abstract class FuzzerWindow : Window
{
    private readonly EventTracer<object?> tracer;
    private readonly List<IFuzzerAddon> addons = [];

    private FuzzerWindow(EventTracer<object?> tracer)
    {
        this.tracer = tracer;
    }

    /// <summary>
    /// Adds an addon to the fuzzer window.
    /// </summary>
    /// <param name="addon">The addon to add.</param>
    public void Add(IFuzzerAddon addon) => this.addons.Add(addon);

    /// <summary>
    /// Initializes the fuzzer window with all addons registered.
    /// </summary>
    public void Initialize()
    {
        // First we register all addons
        foreach (var addon in this.addons) addon.Register(this.tracer);

        // Create a dictionary of views
        var views = this.addons
            .Select(addon => (Name: addon.Name, View: addon.CreateView()))
            .Where(pair => pair.View is not null)
            .ToDictionary(view => view.Name, view => view.View!);

        // Lay them out using the user-defined logic
        var laidOutViews = this.Layout(views);

        // Menu-bar
        var menuBar = this.ConstructMenuBar();

        // Status-bar
        var statusBar = new StatusBar(this.addons
            .Select(addon => addon.CreateStatusItem())
            .OfType<StatusItem>()
            .ToArray());

        // Add all views to the window
        this.Add(laidOutViews
            .Prepend(menuBar)
            .Append(statusBar)
            .ToArray());
    }

    protected abstract IEnumerable<View> Layout(IReadOnlyDictionary<string, View> views);

    private MenuBar ConstructMenuBar()
    {
        // Constructing the menu-bar is quite complex as it requires recursive merging of menu items

        // TODO
        throw new System.NotImplementedException();
    }
}
