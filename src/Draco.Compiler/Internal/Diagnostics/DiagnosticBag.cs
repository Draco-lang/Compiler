using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Diagnostics;

/// <summary>
/// Holds diagnostic messages.
/// </summary>
internal abstract class DiagnosticBag : IReadOnlyCollection<Diagnostic>
{
    /// <summary>
    /// An empty diagnostic bag that stays empty.
    /// </summary>
    public static DiagnosticBag Empty => EmptyDiagnosticBag.Instance;

    /// <summary>
    /// True, if the bag contains errors.
    /// </summary>
    public virtual bool HasErrors => this.Any(d => d.Severity == DiagnosticSeverity.Error);

    public abstract int Count { get; }

    /// <summary>
    /// Adds a diagnostic to this bag.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    public abstract void Add(Diagnostic diagnostic);

    /// <summary>
    /// Removes all diagnostics from this bag.
    /// </summary>
    public abstract void Clear();

    /// <summary>
    /// Adds a range of diagnostics to this bag.
    /// </summary>
    /// <param name="diagnostics">The range of diagnostics to add.</param>
    public virtual void AddRange(IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diag in diagnostics) this.Add(diag);
    }

    public abstract IEnumerator<Diagnostic> GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
