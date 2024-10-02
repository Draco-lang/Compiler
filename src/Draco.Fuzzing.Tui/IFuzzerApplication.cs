using Draco.Fuzzing.Tracing;
using Draco.Fuzzing.Tui.Addons;

namespace Draco.Fuzzing.Tui;

/// <summary>
/// The toplevel application that can have addons registered.
/// </summary>
public interface IFuzzerApplication
{
    /// <summary>
    /// The fuzzer being visualized.
    /// </summary>
    public IFuzzer Fuzzer { get; }

    /// <summary>
    /// The tracer for the fuzzer.
    /// </summary>
    public EventTracer<object?> Tracer { get; }

    /// <summary>
    /// Gets an addon by name.
    /// </summary>
    /// <typeparam name="TAddon">The type of the addon.</typeparam>
    /// <param name="name">The name of the addon.</param>
    /// <returns>The addon with the given name.</returns>
    public TAddon GetAddon<TAddon>(string name) => (TAddon)this.GetAddon(name);

    /// <summary>
    /// Gets an addon by name.
    /// </summary>
    /// <param name="name">The name of the addon.</param>
    /// <returns>The addon with the given name.</returns>
    public IFuzzerAddon GetAddon(string name);
}
