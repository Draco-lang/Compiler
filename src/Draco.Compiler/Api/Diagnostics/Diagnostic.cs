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
/// A template for creating <see cref="Diagnostic"/> messages.
/// </summary>
public sealed record class DiagnosticTemplate
{
    /// <summary>
    /// Creates a new <see cref="DiagnosticTemplate"/>.
    /// </summary>
    /// <param name="title">A short title for the message.</param>
    /// <param name="severity">The severity of the message.</param>
    /// <param name="format">The format string that describes the diagnostic in detail.</param>
    /// <returns>The constructed <see cref="DiagnosticTemplate"/>.</returns>
    public static DiagnosticTemplate Create(string title, DiagnosticSeverity severity, string format) =>
        new(title: title, severity: severity, format: format);

    /// <summary>
    /// A short title for the message.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// The severity of the message.
    /// </summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>
    /// The format string that describes the diagnostic in detail.
    /// </summary>
    public string Format { get; }

    private DiagnosticTemplate(
        string title,
        DiagnosticSeverity severity,
        string format)
    {
        this.Title = title;
        this.Severity = severity;
        this.Format = format;
    }
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
