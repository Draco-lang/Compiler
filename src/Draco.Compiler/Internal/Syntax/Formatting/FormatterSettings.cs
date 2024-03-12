using System.Text;

namespace Draco.Compiler.Internal.Syntax.Formatting;

/// <summary>
/// The settings of the formatter.
/// </summary>
public sealed class FormatterSettings
{
    /// <summary>
    /// The default formatting settings.
    /// </summary>
    public static FormatterSettings Default { get; } = new();

    /// <summary>
    /// The newline sequence.
    /// </summary>
    public string Newline { get; init; } = "\n";

    /// <summary>
    /// The indentation sequence.
    /// </summary>
    public string Indentation { get; init; } = "    ";

    public int LineWidth { get; init; } = 160;

    public string IndentationString(int amount = 1)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < amount; ++i) sb.Append(this.Indentation);
        return sb.ToString();
    }
    public string PaddingString(int width = 1)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < width; ++i) sb.Append(' ');
        return sb.ToString();
    }
}
