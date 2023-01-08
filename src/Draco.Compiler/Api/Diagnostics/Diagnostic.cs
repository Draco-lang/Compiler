using System;
using System.Collections.Immutable;
using System.Text;

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

// NOTE: Eventually we'd want error codes too. For now it's way too early for that.
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
    /// <param name="errorCode">The error code of the error.</param>
    /// <returns>The constructed <see cref="DiagnosticTemplate"/>.</returns>
    public static DiagnosticTemplate Create(string title, DiagnosticSeverity severity, string format, string errorCode) =>
        new(title: title, severity: severity, format: format, errorCode: errorCode);

    // NOTE: error codes are using format DRX###
    // where X is category of the error in form of number and ### is index of the error,
    // every category starts indexing from 1 except of internal compiler error
    // Categories:
    // 0 - Internal compiler error
    // 1 - Syntax error
    // 2 - Symbol resolution error
    // 3 - Type checking error
    // 4 - Dataflow error
    // 5 - Codegen error

    /// <summary>
    /// Creates error code for draco error.
    /// </summary>
    /// <param name="category">Category of the error.</param>
    /// <param name="index">Index of the error.</param>
    /// <returns></returns>
    public static string SyntaxErrorCode(int category, int index) => $"DR{category}{index:D3}";

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

    /// <summary>
    /// The error code of the error.
    /// </summary>
    public string ErrorCode { get; }

    private DiagnosticTemplate(
        string title,
        DiagnosticSeverity severity,
        string format,
        string errorCode)
    {
        this.Title = title;
        this.Severity = severity;
        this.Format = format;
        this.ErrorCode = errorCode;
    }
}

/// <summary>
/// A diagnostic produced by the compiler.
/// </summary>
public sealed class Diagnostic
{
    /// <summary>
    /// Constructs a <see cref="Diagnostic"/> message.
    /// </summary>
    /// <param name="template">The <see cref="DiagnosticTemplate"/> that describes this kind of message.</param>
    /// <param name="location">The location the diagnostic was produced at.</param>
    /// <param name="relatedInformation">Related information about the diagnostic.</param>
    /// <param name="formatArgs">The format arguments of the message.</param>
    /// <returns>The constructed <see cref="Diagnostic"/>.</returns>
    public static Diagnostic Create(
        DiagnosticTemplate template,
        Location location,
        ImmutableArray<DiagnosticRelatedInformation> relatedInformation,
        params object?[] formatArgs) => new(
            template: template,
            location: location,
            formatArgs: formatArgs,
            relatedInformation: relatedInformation);

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
        params object?[] formatArgs) => Create(
            template: template,
            location: location,
            formatArgs: formatArgs,
            relatedInformation: ImmutableArray<DiagnosticRelatedInformation>.Empty);

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
    /// The formatted message.
    /// </summary>
    public string Message => string.Format(this.Format, this.FormatArgs);

    /// <summary>
    /// The assoicated location of the message.
    /// </summary>
    public Location Location { get; }

    /// <summary>
    /// The error code of the error.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Related information to this diagnostic.
    /// </summary>
    public ImmutableArray<DiagnosticRelatedInformation> RelatedInformation { get; }

    internal Diagnostic(
        Internal.Diagnostics.Diagnostic internalDiagnostic,
        Location location,
        ImmutableArray<DiagnosticRelatedInformation> relatedInformation)
        : this(internalDiagnostic.Template, internalDiagnostic.FormatArgs, location, relatedInformation)
    {
    }

    private Diagnostic(
        DiagnosticTemplate template,
        object?[] formatArgs,
        Location location,
        ImmutableArray<DiagnosticRelatedInformation> relatedInformation)
    {
        this.Template = template;
        this.FormatArgs = formatArgs;
        this.Location = location;
        this.RelatedInformation = relatedInformation;
        this.ErrorCode = template.ErrorCode;
    }

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
        sb.Append(severity);
        if (!this.Location.IsNone) sb.Append(' ').Append(this.Location);
        sb.Append(": ").Append(this.Message);
        return sb.ToString();
    }
}

/// <summary>
/// Represents related information about the diagnostic.
/// </summary>
public sealed class DiagnosticRelatedInformation
{
    /// <summary>
    /// Constructs a new <see cref="DiagnosticRelatedInformation"/>.
    /// </summary>
    /// <param name="location">The location of the related information.</param>
    /// <param name="format">The format message.</param>
    /// <param name="formatArgs">The format arguments.</param>
    /// <returns>The constructed <see cref="DiagnosticRelatedInformation"/>.</returns>
    public static DiagnosticRelatedInformation Create(
        Location location,
        string format,
        params object?[] formatArgs) => new(
            location: location,
            format: format,
            formatArgs: formatArgs);

    /// <summary>
    /// The location of the related information.
    /// </summary>
    public Location Location { get; }

    /// <summary>
    /// The format message.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// The format arguments.
    /// </summary>
    public object?[] FormatArgs { get; }

    /// <summary>
    /// The formatted message.
    /// </summary>
    public string Message => string.Format(this.Format, this.FormatArgs);

    internal DiagnosticRelatedInformation(
        Internal.Diagnostics.DiagnosticRelatedInformation internalInfo,
        Location location)
        : this(location, internalInfo.Format, internalInfo.FormatArgs)
    {
    }

    private DiagnosticRelatedInformation(
        Location location,
        string format,
        object?[] formatArgs)
    {
        this.Location = location;
        this.Format = format;
        this.FormatArgs = formatArgs;
    }
}
