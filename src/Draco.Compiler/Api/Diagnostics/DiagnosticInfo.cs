using System.Collections.Immutable;

namespace Draco.Compiler.Api.Diagnostics;

/// <summary>
/// Information about a diagnostic message without location information.
/// </summary>
internal sealed class DiagnosticInfo
{
    /// <summary>
    /// Constructs a <see cref="DiagnosticInfo"/> message.
    /// </summary>
    /// <param name="template">The <see cref="DiagnosticTemplate"/> that describes this kind of message.</param>
    /// <param name="relatedInformation">Related information about the diagnostic.</param>
    /// <param name="formatArgs">The format arguments of the message.</param>
    /// <returns>The constructed <see cref="DiagnosticInfo"/>.</returns>
    public static DiagnosticInfo Create(
        DiagnosticTemplate template,
        ImmutableArray<DiagnosticRelatedInformation> relatedInformation,
        params object?[] formatArgs) => new(
            template: template,
            formatArgs: formatArgs,
            relatedInformation: relatedInformation);

    /// <summary>
    /// Constructs a <see cref="DiagnosticInfo"/> message.
    /// </summary>
    /// <param name="template">The <see cref="DiagnosticTemplate"/> that describes this kind of message.</param>
    /// <param name="formatArgs">The format arguments of the message.</param>
    /// <returns>The constructed <see cref="DiagnosticInfo"/>.</returns>
    public static DiagnosticInfo Create(
        DiagnosticTemplate template,
        params object?[] formatArgs) => Create(
            template: template,
            formatArgs: formatArgs,
            relatedInformation: ImmutableArray<DiagnosticRelatedInformation>.Empty);

    /// <summary>
    /// The template for this message.
    /// </summary>
    public DiagnosticTemplate Template { get; }

    /// <summary>
    /// The format arguments to apply to the message.
    /// </summary>
    public object?[] FormatArgs { get; }

    /// <summary>
    /// Related information to this diagnostic.
    /// </summary>
    public ImmutableArray<DiagnosticRelatedInformation> RelatedInformation { get; }

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
    /// The formatted message.
    /// </summary>
    public string Message => string.Format(this.Format, this.FormatArgs);

    private DiagnosticInfo(
        DiagnosticTemplate template,
        object?[] formatArgs,
        ImmutableArray<DiagnosticRelatedInformation> relatedInformation)
    {
        this.Template = template;
        this.FormatArgs = formatArgs;
        this.RelatedInformation = relatedInformation;
    }
}
