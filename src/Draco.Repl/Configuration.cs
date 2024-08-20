using System.Collections.Generic;
using Draco.Compiler.Api.Diagnostics;
using PrettyPrompt.Highlighting;

namespace Draco.Repl;

internal sealed class Configuration
{
    public string Prompt { get; set; } = "> ";
    public ColorScheme<InterfaceColors> InterfaceColors { get; set; } = ConfigurationDefaults.GetInterfaceColors();
    public List<string> DefaultImports { get; set; } = [
        "System",
        "System.Collections.Generic",
        "System.Linq"];

    public FormattedString GetFormattedPrompt() =>
        new(this.Prompt, new FormatSpan(0, this.Prompt.Length, this.InterfaceColors.Get(Repl.InterfaceColors.PromptColor)));

    public FormattedString GetFormattedDiagnostic(Diagnostic diagnostic)
    {
        var diagString = diagnostic.ToString();
        var diagColor = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => this.InterfaceColors.Get(Repl.InterfaceColors.ErrorColor),
            _ => this.InterfaceColors.Default,
        };
        return new(diagString, new FormatSpan(0, diagString.Length, diagColor));
    }
}
