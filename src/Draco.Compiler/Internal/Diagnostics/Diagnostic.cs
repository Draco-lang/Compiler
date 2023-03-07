using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using ApiDiagnostic = Draco.Compiler.Api.Diagnostics.Diagnostic;
using ApiDiagnosticRelatedInformation = Draco.Compiler.Api.Diagnostics.DiagnosticRelatedInformation;

namespace Draco.Compiler.Internal.Diagnostics;

/// <summary>
/// Represents a diagnostic message produced by the compiler.
/// </summary>
internal sealed partial class Diagnostic
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
        Location? location,
        ImmutableArray<DiagnosticRelatedInformation> relatedInformation)
    {
        this.Template = template;
        this.FormatArgs = formatArgs;
        this.Location = location ?? Location.None;
        this.RelatedInformation = relatedInformation;
    }

    /// <summary>
    /// Translates this <see cref="Diagnostic"/> to an <see cref="ApiDiagnostic"/>.
    /// </summary>
    /// <param name="context">The <see cref="SyntaxNode"/> node this <see cref="Diagnostic"/> is attached to.</param>
    /// <returns>The equivalent <see cref="ApiDiagnostic"/> to <paramref name="context"/>.</returns>
    public ApiDiagnostic ToApiDiagnostic(SyntaxNode? context) => new(
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
    /// <param name="context">The <see cref="SyntaxNode"/> node this <see cref="DiagnosticRelatedInformation"/> is attached to.</param>
    /// <returns>The equivalent <see cref="ApiDiagnosticRelatedInformation"/> to <paramref name="context"/>.</returns>
    public ApiDiagnosticRelatedInformation ToApiDiagnosticRelatedInformation(SyntaxNode? context) =>
        new(this, this.Location.ToApiLocation(context));
}

internal sealed partial class Diagnostic
{
    /// <summary>
    /// A builder type for <see cref="Diagnostic"/>.
    /// </summary>
    public sealed class Builder
    {
        public DiagnosticTemplate? Template { get; private set; }
        public object?[]? FormatArgs { get; private set; }
        public Location? Location { get; private set; }
        public ImmutableArray<DiagnosticRelatedInformation>.Builder? RelatedInformation { get; private set; }

        /// <summary>
        /// Attempts to build a <see cref="Diagnostic"/>, if at least <see cref="Template"/> is filled.
        /// </summary>
        /// <param name="result">The built <see cref="Diagnostic"/>.</param>
        /// <returns>True, if at least <see cref="Template"/> is filled and a <see cref="Diagnostic"/> was built.</returns>
        public bool TryBuild([MaybeNullWhen(false)] out Diagnostic result)
        {
            if (this.Template is null)
            {
                result = null;
                return false;
            }
            result = Create(
                template: this.Template,
                location: this.Location,
                formatArgs: this.FormatArgs ?? Array.Empty<object?>(),
                relatedInformation: this.RelatedInformation?.ToImmutable() ?? ImmutableArray<DiagnosticRelatedInformation>.Empty);
            return true;
        }

        /// <summary>
        /// Builds a <see cref="Diagnostic"/>.
        /// </summary>
        /// <returns>The built <see cref="Diagnostic"/>.</returns>
        public Diagnostic Build()
        {
            if (!this.TryBuild(out var result)) throw new InvalidOperationException("Diagnostic builder missing the Template");
            return result;
        }

        public Builder WithTemplate(DiagnosticTemplate template)
        {
            this.Template = template;
            return this;
        }

        public Builder WithFormatArgs(params object?[] args)
        {
            this.FormatArgs = args;
            return this;
        }

        public Builder WithLocation(Location location)
        {
            this.Location = location;
            return this;
        }

        public Builder WithRelatedInformation(ImmutableArray<DiagnosticRelatedInformation> relatedInformation)
        {
            this.RelatedInformation = relatedInformation.ToBuilder();
            return this;
        }

        public Builder WithMessage(DiagnosticTemplate template, params object?[] args) => this
            .WithTemplate(template)
            .WithFormatArgs(args);

        public Builder WithRelatedInformation(DiagnosticRelatedInformation relatedInformation)
        {
            this.RelatedInformation ??= ImmutableArray.CreateBuilder<DiagnosticRelatedInformation>();
            this.RelatedInformation.Add(relatedInformation);
            return this;
        }

        public Builder WithRelatedInformation(Location location, string format, params object?[] formatArgs) => this
            .WithRelatedInformation(new DiagnosticRelatedInformation(location, format, formatArgs));

        public Builder WithRelatedInformation(string format, params object?[] formatArgs) => this
            .WithRelatedInformation(Location.None, format, formatArgs);
    }
}
