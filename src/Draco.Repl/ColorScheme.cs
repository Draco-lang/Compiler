using System.Collections.Generic;
using PrettyPrompt.Highlighting;

namespace Draco.Repl;

internal sealed class ColorScheme<TKey>
    where TKey : notnull
{
    public AnsiColor Default { get; init; } = AnsiColor.White;

    private readonly Dictionary<TKey, AnsiColor> colors = [];

    public AnsiColor Get(TKey key) => this.colors.TryGetValue(key, out var color) ? color : this.Default;
    public void Set(TKey key, AnsiColor color) => this.colors[key] = color;
}
