using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Diagnostics;

/// <summary>
/// The possible severities of diagnostic messages.
/// </summary>
internal enum DiagnosticSeverity
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
internal sealed record class DiagnosticTemplate
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
/// Represents a diagnostic message produced by the compiler.
/// </summary>
internal sealed record class Diagnostic
{
    /// <summary>
    /// Constructs a <see cref="Diagnostic"/> message.
    /// </summary>
    /// <param name="template">The <see cref="DiagnosticTemplate"/> that describes this kind of message.</param>
    /// <param name="location">The location the diagnostic was produced at.</param>
    /// <returns>The constructed <see cref="Diagnostic"/>.</returns>
    public static Diagnostic Create(
        DiagnosticTemplate template,
        Location location) => new(template: template, formatArgs: Array.Empty<object?>(), location: location);

    /// <summary>
    /// Constructs a <see cref="Diagnostic"/> message.
    /// </summary>
    /// <param name="template">The <see cref="DiagnosticTemplate"/> that describes this kind of message.</param>
    /// <param name="location">The location the diagnostic was produced at.</param>
    /// <param name="formatArgs">The format arguments of the message.</param>
    /// <returns>The constructed <see cref="Diagnostic"/>.</returns>
    public static Diagnostic Create(
        DiagnosticTemplate template,
        Location location,
        params object?[] formatArgs) => Create(template, location, formatArgs);

    /// <summary>
    /// The template for this message.
    /// </summary>
    public DiagnosticTemplate Template { get; }

    /// <summary>
    /// A short title for the message.
    /// </summary>
    public string Title => this.Template.Title;

    /// <summary>
    /// The severity of the message.
    /// </summary>
    public DiagnosticSeverity Severity => this.Template.Severity;

    /// <summary>
    /// The format string that describes the diagnostic in detail.
    /// </summary>
    public string Format => this.Template.Format;

    /// <summary>
    /// The format arguments to apply to the message.
    /// </summary>
    public object?[] FormatArgs { get; }

    /// <summary>
    /// The assoicated location of the message.
    /// </summary>
    public Location Location { get; }

    private Diagnostic(
        DiagnosticTemplate template,
        object?[] formatArgs,
        Location location)
    {
        this.Template = template;
        this.FormatArgs = formatArgs;
        this.Location = location;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder();
        var severity = this.Severity switch
        {
            DiagnosticSeverity.Info => "info",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Error => "error",
            _ => throw new InvalidOperationException(),
        };
        sb.AppendLine($"{severity}: {this.Title}");
        var desc = string.Format(this.Format, this.FormatArgs);
        sb.Append(desc);
        return sb.ToString();
    }
}
