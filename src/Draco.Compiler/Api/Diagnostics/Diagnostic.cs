using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Diagnostics;

/// <summary>
/// The possible severities of diagnostic messages.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// Informational diagnostic message.
    /// </summary>
    Info,

    /// <summary>
    /// Warning diagnostic message.
    /// </summary>
    Warning,

    /// <summary>
    /// Error diagnostic message.
    /// </summary>
    Error,
}

/// <summary>
/// A diagnostic produced by the compiler.
/// </summary>
public sealed class Diagnostic
{
    /// <summary>
    /// The location of the diagnostic.
    /// </summary>
    public Location? Location { get; }

    /// <summary>
    /// The severity of the message.
    /// </summary>
    public DiagnosticSeverity Severity => this.internalDiagnostic.Severity;

    /// <summary>
    /// The message explaining this diagnostics.
    /// </summary>
    public string Message =>
        string.Format(this.internalDiagnostic.Format, this.internalDiagnostic.FormatArgs);

    private readonly Internal.Diagnostics.Diagnostic internalDiagnostic;

    internal Diagnostic(
        Internal.Diagnostics.Diagnostic internalDiagnostic,
        Location? location)
    {
        this.internalDiagnostic = internalDiagnostic;
        this.Location = location;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        var severity = this.internalDiagnostic.Severity switch
        {
            DiagnosticSeverity.Info => "info",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Error => "error",
            _ => throw new InvalidOperationException(),
        };
        var position = this.Location?.Range.Start;
        sb.Append(severity);
        if (position is not null) sb.Append($" at line {position.Value.Line + 1}, column {position.Value.Column + 1}");
        sb.AppendLine($": {this.internalDiagnostic.Title}");
        sb.Append(this.Message);
        return sb.ToString();
    }
}
