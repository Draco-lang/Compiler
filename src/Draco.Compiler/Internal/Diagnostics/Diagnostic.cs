using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using ApiDiagnostic = Draco.Compiler.Api.Diagnostics.Diagnostic;

namespace Draco.Compiler.Internal.Diagnostics;

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
    /// <param name="formatArgs">The format arguments of the message.</param>
    /// <returns>The constructed <see cref="Diagnostic"/>.</returns>
    public static Diagnostic Create(
        DiagnosticTemplate template,
        Location location,
        params object?[] formatArgs) => new(
            template: template,
            location: location,
            formatArgs: formatArgs);

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

    /// <summary>
    /// Translates this <see cref="Diagnostic"/> to an <see cref="ApiDiagnostic"/>.
    /// </summary>
    /// <param name="context">The <see cref="ParseTree"/> node this <see cref="Diagnostic"/> is attached to.</param>
    /// <returns>The equivalent <see cref="ApiDiagnostic"/> to <paramref name="context"/>.</returns>
    public ApiDiagnostic ToApiDiagnostic(ParseTree context) =>
        new(this, this.Location.ToApiLocation(context));
}
