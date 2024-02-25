using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Diagnostics;

/// <summary>
/// Holds diagnostic messages in a concurrent context.
/// </summary>
internal sealed class ConcurrentDiagnosticBag : DiagnosticBag
{
    public override int Count => this.diagnostics.Count;

    private readonly ConcurrentBag<Diagnostic> diagnostics = new();

    public override void Add(Diagnostic diagnostic) => this.diagnostics.Add(diagnostic);
    public override void Clear() => this.diagnostics.Clear();
    public override IEnumerator<Diagnostic> GetEnumerator() => this.diagnostics.GetEnumerator();
}
