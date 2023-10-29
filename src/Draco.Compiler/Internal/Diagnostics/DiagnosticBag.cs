using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Diagnostics;

/// <summary>
/// Holds diagnostic messages.
/// </summary>
internal sealed class DiagnosticBag : IReadOnlyCollection<Diagnostic>
{
    /// <summary>
    /// True, if the bad contains errors.
    /// </summary>
    public bool HasErrors => this.diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    public int Count => this.diagnostics.Count;

    private readonly ConcurrentBag<Diagnostic> diagnostics = new();

    /// <summary>
    /// Adds a diagnostic to this bag.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    public void Add(Diagnostic diagnostic) => this.diagnostics.Add(diagnostic);

    /// <summary>
    /// Adds a range of diagnostics to this bag.
    /// </summary>
    /// <param name="diagnostics">The range of diagnostics to add.</param>
    public void AddRange(IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diag in diagnostics) this.Add(diag);
    }

    public void Clear() => this.diagnostics.Clear();

    public IEnumerator<Diagnostic> GetEnumerator() => this.diagnostics.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
