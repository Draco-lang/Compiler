using Terminal.Gui;

namespace Draco.Fuzzing.Tui.Addons;

/// <summary>
/// Addon for a fuzzer.
/// </summary>
public interface IFuzzerAddon
{
    /// <summary>
    /// The name of the addon.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Registers the addon onto a tracer.
    /// </summary>
    /// <param name="application">The application to register onto.</param>
    public void Register(IFuzzerApplication application);

    /// <summary>
    /// Creates a view for the addon, if applicable.
    /// </summary>
    /// <returns>The view, if applicable.</returns>
    public View? CreateView();

    /// <summary>
    /// Creates a menu bar item for the addon, if applicable.
    /// </summary>
    /// <returns>The menu bar item, if applicable.</returns>
    public MenuBarItem? CreateMenuBarItem();

    /// <summary>
    /// Creates a status item for the addon, if applicable.
    /// </summary>
    /// <returns>The status item, if applicable.</returns>
    public StatusItem? CreateStatusItem();
}
