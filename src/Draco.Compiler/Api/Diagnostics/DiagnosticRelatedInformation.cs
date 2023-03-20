using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Diagnostics;

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

    internal DiagnosticRelatedInformation(
        Location location,
        string format,
        object?[] formatArgs)
    {
        this.Location = location;
        this.Format = format;
        this.FormatArgs = formatArgs;
    }
}
