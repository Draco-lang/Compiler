using Draco.Fuzzing.Tracing;

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
    /// Requires the existence of an addon already registered.
    /// </summary>
    /// <typeparam name="TAddon">The type of the addon.</typeparam>
    /// <param name="name">The name of the addon.</param>
    /// <param name="by">The name of the component requiring the addon for identification.</param>
    /// <returns>The retrieved addon.</returns>
    public TAddon RequireAddon<TAddon>(string name, string by);
}
