using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Holds diagnostic messages.
/// </summary>
internal sealed class DiagnosticBag
{
    /// <summary>
    /// True, if the bad contains errors.
    /// </summary>
    public bool HasErrors { get; private set; }

    private readonly List<Diagnostic> diagnostics = new();

    /// <summary>
    /// Adds a diagnostic to this bag.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    public void Add(Diagnostic diagnostic)
    {
        this.diagnostics.Add(diagnostic);
        this.HasErrors = this.HasErrors
                      || diagnostic.Severity == Api.Diagnostics.DiagnosticSeverity.Error;
    }
}
