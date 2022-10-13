using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler;

/// <summary>
/// The possible severities of diagnostic messages.
/// </summary>
internal enum DiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

// NOTE: This is not a nice location info, but for now in a single file it will suffice
// Just make sure that all components construct errors with a helper method, so the location
// has to be changed in one place instead of a thousand
/// <summary>
/// Represents a diagnostic message produced by the compiler.
/// </summary>
/// <param name="Severity">The severity of this diagnostic message.</param>
/// <param name="Message">A description of the diagnostic.</param>
/// <param name="Location">The location the message is associated to.</param>
internal sealed record class Diagnostic(DiagnosticSeverity Severity, string Message, int Location);
