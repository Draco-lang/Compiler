using PrettyPrompt.Highlighting;

namespace Draco.Repl;

internal static class ConfigurationDefaults
{
    public static ColorScheme<InterfaceColors> GetInterfaceColors()
    {
        var colors = new ColorScheme<InterfaceColors>();
        colors.Set(InterfaceColors.PromptColor, AnsiColor.White);
        colors.Set(InterfaceColors.ErrorColor, AnsiColor.BrightRed);
        return colors;
    }
}
