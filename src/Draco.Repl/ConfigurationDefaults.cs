using Draco.Compiler.Api.Syntax;
using PrettyPrompt.Highlighting;

namespace Draco.Repl;

/// <summary>
/// Default configuration values.
/// </summary>
internal static class ConfigurationDefaults
{
    /// <summary>
    /// Retrieves the default interface colors.
    /// </summary>
    /// <returns>The default interface colors.</returns>
    public static ColorScheme<InterfaceColor> GetInterfaceColors()
    {
        var colors = new ColorScheme<InterfaceColor>();
        colors.Set(InterfaceColor.PromptColor, AnsiColor.White);
        colors.Set(InterfaceColor.ErrorColor, AnsiColor.BrightRed);
        return colors;
    }

    /// <summary>
    /// Retrieves the default syntax colors.
    /// </summary>
    /// <returns>The default syntax colors.</returns>
    public static ColorScheme<SyntaxColoring> GetSyntaxColors()
    {
        var colors = new ColorScheme<SyntaxColoring>();
        colors.Set(SyntaxColoring.LineComment, AnsiColor.BrightGreen);
        colors.Set(SyntaxColoring.DeclarationKeyword, AnsiColor.BrightBlue);
        return colors;
    }
}
