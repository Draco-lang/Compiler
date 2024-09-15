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

        colors.Set(SyntaxColoring.LineComment, AnsiColor.Rgb(87, 166, 74));
        colors.Set(SyntaxColoring.DeclarationKeyword, AnsiColor.Rgb(96, 139, 78));

        var stringLiteralColor = AnsiColor.Rgb(214, 157, 133);
        colors.Set(SyntaxColoring.StringQuotes, stringLiteralColor);
        colors.Set(SyntaxColoring.StringContent, stringLiteralColor);
        colors.Set(SyntaxColoring.CharacterQuotes, stringLiteralColor);
        colors.Set(SyntaxColoring.CharacterContent, stringLiteralColor);

        var escapeSeqColor = AnsiColor.Rgb(255, 214, 142);
        colors.Set(SyntaxColoring.InterpolationQuotes, escapeSeqColor);
        colors.Set(SyntaxColoring.EscapeSequence, escapeSeqColor);

        var declarationColor = AnsiColor.Rgb(86, 156, 214);
        colors.Set(SyntaxColoring.DeclarationKeyword, declarationColor);
        colors.Set(SyntaxColoring.VisibilityKeyword, declarationColor);
        colors.Set(SyntaxColoring.ControlFlowKeyword, AnsiColor.Rgb(215, 160, 223));

        colors.Set(SyntaxColoring.BooleanLiteral, declarationColor);
        colors.Set(SyntaxColoring.NumberLiteral, AnsiColor.Rgb(181, 206, 168));

        colors.Set(SyntaxColoring.FunctionName, AnsiColor.Rgb(220, 220, 170));
        colors.Set(SyntaxColoring.ReferenceTypeName, AnsiColor.Rgb(78, 201, 176));
        colors.Set(SyntaxColoring.ValueTypeName, AnsiColor.Rgb(115, 194, 145));

        var variableColor = AnsiColor.Rgb(156, 217, 240);
        colors.Set(SyntaxColoring.VariableName, variableColor);
        colors.Set(SyntaxColoring.ParameterName, variableColor);
        colors.Set(SyntaxColoring.FieldName, AnsiColor.BrightWhite);
        colors.Set(SyntaxColoring.PropertyName, AnsiColor.BrightWhite);
        colors.Set(SyntaxColoring.ModuleName, AnsiColor.BrightWhite);

        colors.Set(SyntaxColoring.Punctuation, AnsiColor.BrightWhite);
        colors.Set(SyntaxColoring.Operator, AnsiColor.BrightWhite);
        colors.Set(SyntaxColoring.Parenthesis, AnsiColor.BrightWhite);

        return colors;
    }
}
