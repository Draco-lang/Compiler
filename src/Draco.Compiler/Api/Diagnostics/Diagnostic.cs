using System;
using System.Collections.Immutable;
using System.Text;

namespace Draco.Compiler.Api.Diagnostics;

/// <summary>
/// A diagnostic produced by the compiler.
/// </summary>
public sealed partial class Diagnostic
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
        Location? location,
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
        Location? location,
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
    /// The code of the diagnostic.
    /// </summary>
    public string Code => this.Template.Code;

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
        Location? location,
        ImmutableArray<DiagnosticRelatedInformation> relatedInformation)
    {
        this.Template = template;
        this.FormatArgs = formatArgs;
        this.Location = location ?? Location.None;
        this.RelatedInformation = relatedInformation;
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
