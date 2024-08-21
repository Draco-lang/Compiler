using Draco.Compiler.Api.Syntax;
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

    public static ColorScheme<SyntaxColoring> GetSyntaxColors()
    {
        var colors = new ColorScheme<SyntaxColoring>();

        colors.Set(SyntaxColoring.LineComment, AnsiColor.Green);
        colors.Set(SyntaxColoring.DocumentationComment, AnsiColor.Green);

        colors.Set(SyntaxColoring.StringQuotes, AnsiColor.BrightYellow);
        colors.Set(SyntaxColoring.CharacterQuotes, AnsiColor.BrightYellow);
        colors.Set(SyntaxColoring.StringContent, AnsiColor.BrightYellow);
        colors.Set(SyntaxColoring.CharacterContent, AnsiColor.BrightYellow);

        colors.Set(SyntaxColoring.EscapeSequence, AnsiColor.BrightRed);
        colors.Set(SyntaxColoring.InterpolationQuotes, AnsiColor.BrightRed);

        colors.Set(SyntaxColoring.DeclarationKeyword, AnsiColor.BrightBlue);
        colors.Set(SyntaxColoring.ControlFlowKeyword, AnsiColor.BrightMagenta);
        colors.Set(SyntaxColoring.VisibilityKeyword, AnsiColor.BrightBlue);

        colors.Set(SyntaxColoring.BooleanLiteral, AnsiColor.BrightBlue);
        colors.Set(SyntaxColoring.NumberLiteral, AnsiColor.BrightGreen);

        colors.Set(SyntaxColoring.ReferenceTypeName, AnsiColor.BrightCyan);
        colors.Set(SyntaxColoring.ValueTypeName, AnsiColor.BrightGreen);

        colors.Set(SyntaxColoring.FunctionName, AnsiColor.BrightYellow);

        return colors;
    }
}
