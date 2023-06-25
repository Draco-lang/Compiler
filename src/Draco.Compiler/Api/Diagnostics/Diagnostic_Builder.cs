using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Draco.Compiler.Api.Diagnostics;

public sealed partial class Diagnostic
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
                relatedInformation: this.RelatedInformation?.ToImmutable()
                                 ?? ImmutableArray<DiagnosticRelatedInformation>.Empty);
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

        public Builder WithRelatedInformation(Location? location, string format, params object?[] formatArgs) => this
            .WithRelatedInformation(DiagnosticRelatedInformation.Create(
                location: location,
                format: format,
                formatArgs: formatArgs));

        public Builder WithRelatedInformation(string format, params object?[] formatArgs) => this
            .WithRelatedInformation(Location.None, format, formatArgs);
    }
}
