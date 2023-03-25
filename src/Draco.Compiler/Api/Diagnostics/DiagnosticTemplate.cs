using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Api.Diagnostics;

/// <summary>
/// A template for creating <see cref="Diagnostic"/> messages.
/// </summary>
public sealed record class DiagnosticTemplate
{
    /// <summary>
    /// Creates a new <see cref="DiagnosticTemplate"/>.
    /// </summary>
    /// <param name="code">The code of the diagnostic.</param>
    /// <param name="title">A short title for the message.</param>
    /// <param name="severity">The severity of the message.</param>
    /// <param name="format">The format string that describes the diagnostic in detail.</param>
    /// <returns>The constructed <see cref="DiagnosticTemplate"/>.</returns>
    public static DiagnosticTemplate Create(string code, string title, DiagnosticSeverity severity, string format) =>
        new(code: code, title: title, severity: severity, format: format);

    // NOTE: diagnostic codes are using format DRX###
    // where X is category of the diagnostic in the form of a digit and ### is index of the diagnostic,
    // every category starts indexing from 1 except of internal compiler errors

    /// <summary>
    /// Creates a diagnostic code for a Draco diagnostic.
    /// </summary>
    /// <param name="category">Category of the diagnostic.</param>
    /// <param name="index">Index of the diagnostic.</param>
    /// <returns>The constructed diagnostic code.</returns>
    internal static string CreateDiagnosticCode(DiagnosticCategory category, int index) => $"DR{(int)category}{index:D3}";

    /// <summary>
    /// The code of the diagnostic.
    /// </summary>
    public string Code { get; }

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
        string code,
        string title,
        DiagnosticSeverity severity,
        string format)
    {
        this.Code = code;
        this.Title = title;
        this.Severity = severity;
        this.Format = format;
    }
}
