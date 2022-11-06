using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Diagnostics;

/// <summary>
/// A diagnostic produced by the compiler.
/// </summary>
public sealed class Diagnostic
{
    /// <summary>
    /// The location of the diagnostic.
    /// </summary>
    public Location Location { get; }

    /// <summary>
    /// The message explaining this diagnostics.
    /// </summary>
    public string Message =>
        string.Format(this.internalDiagnostic.Format, this.internalDiagnostic.FormatArgs);

    private readonly Internal.Diagnostics.Diagnostic internalDiagnostic;

    internal Diagnostic(
        Internal.Diagnostics.Diagnostic internalDiagnostic,
        Location location)
    {
        this.internalDiagnostic = internalDiagnostic;
        this.Location = location;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        var severity = this.internalDiagnostic.Severity switch
        {
            Internal.Diagnostics.DiagnosticSeverity.Info => "info",
            Internal.Diagnostics.DiagnosticSeverity.Warning => "warning",
            Internal.Diagnostics.DiagnosticSeverity.Error => "error",
            _ => throw new InvalidOperationException(),
        };
        var position = this.Location.Range.Start;
        sb.AppendLine($"{severity} at line {position.Line + 1}, column {position.Column + 1}: {this.internalDiagnostic.Title}");
        sb.Append(this.Message);
        return sb.ToString();
    }
}
