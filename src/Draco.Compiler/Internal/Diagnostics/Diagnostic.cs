using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using ApiDiagnostic = Draco.Compiler.Api.Diagnostics.Diagnostic;
using ApiDiagnosticRelatedInformation = Draco.Compiler.Api.Diagnostics.DiagnosticRelatedInformation;

namespace Draco.Compiler.Internal.Diagnostics;

/// <summary>
/// Represents a diagnostic message produced by the compiler.
/// </summary>
internal sealed class Diagnostic
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
    /// The assoicated location of the message.
    /// </summary>
    public Location Location { get; }

    /// <summary>
    /// Additional information for this diagnostic.
    /// </summary>
    public ImmutableArray<DiagnosticRelatedInformation> RelatedInformation { get; }

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
    }

    /// <summary>
    /// Translates this <see cref="Diagnostic"/> to an <see cref="ApiDiagnostic"/>.
    /// </summary>
    /// <param name="context">The <see cref="ParseTree"/> node this <see cref="Diagnostic"/> is attached to.</param>
    /// <returns>The equivalent <see cref="ApiDiagnostic"/> to <paramref name="context"/>.</returns>
    public ApiDiagnostic ToApiDiagnostic(ParseTree? context) => new(
        this,
        this.Location.ToApiLocation(context),
        this.RelatedInformation.Select(i => i.ToApiDiagnosticRelatedInformation(null)).ToImmutableArray());
}

/// <summary>
/// Extra information for diagnostics.
/// </summary>
/// <param name="Location">The location of the related info.</param>
/// <param name="Format">The format message.</param>
/// <param name="FormatArgs">The format arguments.</param>
internal sealed record class DiagnosticRelatedInformation(
    Location Location,
    string Format,
    object?[] FormatArgs)
{
    /// <summary>
    /// Translates this <see cref="DiagnosticRelatedInformation"/> to an <see cref="ApiDiagnosticRelatedInformation"/>.
    /// </summary>
    /// <param name="context">The <see cref="ParseTree"/> node this <see cref="DiagnosticRelatedInformation"/> is attached to.</param>
    /// <returns>The equivalent <see cref="ApiDiagnosticRelatedInformation"/> to <paramref name="context"/>.</returns>
    public ApiDiagnosticRelatedInformation ToApiDiagnosticRelatedInformation(ParseTree? context) =>
        new(this, this.Location.ToApiLocation(context));
}
