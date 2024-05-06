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
}
