using System.Collections.Generic;
using PrettyPrompt.Highlighting;

namespace Draco.Repl;

/// <summary>
/// A type to associate colors with keys.
/// </summary>
/// <typeparam name="TKey">The color key type.</typeparam>
internal sealed class ColorScheme<TKey>
    where TKey : notnull
{
    /// <summary>
    /// The default color.
    /// </summary>
    public ConsoleFormat Default { get; init; } = new(Foreground: AnsiColor.White);

    private readonly Dictionary<TKey, ConsoleFormat> colors = [];

    /// <summary>
    /// Retrieves the color associated with the given key.
    /// </summary>
    /// <param name="key">The key to retrieve the color for.</param>
    /// <returns>The color associated with <paramref name="key"/>, or <see cref="Default"/> if
    /// no color is associated with <paramref name="key"/>.</returns>
    public ConsoleFormat Get(TKey key) => this.colors.TryGetValue(key, out var color) ? color : this.Default;

    /// <summary>
    /// Sets the color associated with the given key.
    /// </summary>
    /// <param name="key">The key to associate the color with.</param>
    /// <param name="color">The color to associate with <paramref name="key"/>.</param>
    public void Set(TKey key, ConsoleFormat color) => this.colors[key] = color;

    /// <summary>
    /// Sets the color associated with the given key.
    /// </summary>
    /// <param name="key">The key to associate the color with.</param>
    /// <param name="color">The color to associate with <paramref name="key"/>.</param>
    public void Set(TKey key, AnsiColor color) => this.Set(key, new ConsoleFormat(Foreground: color));
}
