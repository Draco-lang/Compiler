using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Diagnostics;

/// <summary>
/// Holds diagnostic messages.
/// </summary>
internal sealed class DiagnosticBag : ICollection<Diagnostic>
{
    /// <summary>
    /// True, if the bad contains errors.
    /// </summary>
    public bool HasErrors { get; private set; }

    public int Count => this.diagnostics.Count;
    public bool IsReadOnly => false;

    private readonly List<Diagnostic> diagnostics = new();

    public void Add(Diagnostic diagnostic)
    {
        this.diagnostics.Add(diagnostic);
        this.HasErrors = this.HasErrors
                      || diagnostic.Severity == Api.Diagnostics.DiagnosticSeverity.Error;
    }

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

    bool ICollection<Diagnostic>.Contains(Diagnostic item) => this.diagnostics.Contains(item);
    void ICollection<Diagnostic>.CopyTo(Diagnostic[] array, int arrayIndex) => this.diagnostics.CopyTo(array, arrayIndex);
    bool ICollection<Diagnostic>.Remove(Diagnostic item) => this.diagnostics.Remove(item);
}
