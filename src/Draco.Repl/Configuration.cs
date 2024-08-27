using System.Collections.Generic;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using PrettyPrompt.Highlighting;

namespace Draco.Repl;

/// <summary>
/// Represents the configuration of the REPL.
/// </summary>
internal sealed class Configuration
{
    /// <summary>
    /// The prompt text.
    /// </summary>
    public string Prompt { get; set; } = "> ";

    /// <summary>
    /// True if the REPL should use the MSBuild diagnostic format.
    /// </summary>
    public bool MsbuildDiagFormat { get; set; }

    /// <summary>
    /// The colors used in the REPL interface.
    /// </summary>
    public ColorScheme<InterfaceColor> InterfaceColors { get; set; } = ConfigurationDefaults.GetInterfaceColors();

    /// <summary>
    /// The colors used for syntax highlighting.
    /// </summary>
    public ColorScheme<SyntaxColoring> SyntaxColors { get; set; } = ConfigurationDefaults.GetSyntaxColors();

    /// <summary>
    /// The default imports for the REPL session.
    /// </summary>
    public List<string> DefaultImports { get; set; } = [
        "System",
        "System.Collections.Generic",
        "System.Linq"];

    /// <summary>
    /// Utility to get the formatted prompt string.
    /// </summary>
    /// <returns>The formatted prompt string.</returns>
    public FormattedString GetFormattedPrompt() =>
        new(this.Prompt, new FormatSpan(0, this.Prompt.Length, this.InterfaceColors.Get(InterfaceColor.PromptColor)));

    /// <summary>
    /// Utility to get the formatted diagnostic string.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to format.</param>
    /// <returns>The formatted diagnostic string.</returns>
    public FormattedString GetFormattedDiagnostic(Diagnostic diagnostic)
    {
        var diagString = this.MsbuildDiagFormat ? diagnostic.ToMsbuildString() : diagnostic.ToString();
        var diagColor = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => this.InterfaceColors.Get(InterfaceColor.ErrorColor),
            _ => this.InterfaceColors.Default,
        };
        return new(diagString, new FormatSpan(0, diagString.Length, diagColor));
    }
}
