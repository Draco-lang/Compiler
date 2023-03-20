using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
