using System.Collections.Generic;
using PrettyPrompt.Highlighting;

namespace Draco.Repl;

internal sealed class ColorScheme<TKey>
    where TKey : notnull
{
    public ConsoleFormat Default { get; init; } = new(Foreground: AnsiColor.White);

    private readonly Dictionary<TKey, ConsoleFormat> colors = [];

    public ConsoleFormat Get(TKey key) => this.colors.TryGetValue(key, out var color) ? color : this.Default;
    public void Set(TKey key, ConsoleFormat color) => this.colors[key] = color;
    public void Set(TKey key, AnsiColor color) => this.Set(key, new ConsoleFormat(Foreground: color));
}
