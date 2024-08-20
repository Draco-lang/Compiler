using PrettyPrompt.Highlighting;

namespace Draco.Repl;

internal static class ConfigurationDefaults
{
    public static ColorScheme<InterfaceColor> GetInterfaceColors()
    {
        var colors = new ColorScheme<InterfaceColor>();
        colors.Set(InterfaceColor.PromptColor, AnsiColor.White);
        colors.Set(InterfaceColor.ErrorColor, AnsiColor.BrightRed);
        return colors;
    }

    public static ColorScheme<SyntaxColor> GetSyntaxColors()
    {
        var colors = new ColorScheme<SyntaxColor>();
        colors.Set(SyntaxColor.Comment, AnsiColor.BrightGreen);
        colors.Set(SyntaxColor.Keyword, AnsiColor.BrightBlue);
        colors.Set(SyntaxColor.String, AnsiColor.BrightYellow);
        colors.Set(SyntaxColor.Type, AnsiColor.BrightCyan);
        colors.Set(SyntaxColor.Name, AnsiColor.White);
        return colors;
    }
}
