using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Diagnostics;

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
        params object?[] formatArgs) => new(template: template, location: location, formatArgs: formatArgs);

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

    public bool Equals(Diagnostic? other)
    {
        //throw new Exception();
        if (other is null || other is not Diagnostic diag) return false;
        if (this.Title != diag.Title) return false;
        if (this.Severity != diag.Severity) return false;
        if (this.Format != diag.Format) return false;
        if (this.FormatArgs.Length != diag.FormatArgs.Length) return false;
        for (int i = 0; i < diag.FormatArgs.Length; i++)
        {
            if (this.FormatArgs[i] != diag.FormatArgs[i]) return false;
        }
        if (this.Location.Range.Width != this.Location.Range.Width) return false;
        if (this.Location.Range.Offset != this.Location.Range.Offset) return false;
        return true;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(this.Title);
        hash.Add(this.Severity);
        hash.Add(this.Format);
        hash.Add(this.Location.Range.Width);
        hash.Add(this.Location.Range.Offset);
        foreach (var arg in this.FormatArgs)
        {
            hash.Add(arg);
        }
        return hash.ToHashCode();
    }
}
