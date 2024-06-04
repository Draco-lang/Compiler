using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Diagnostics;

/// <summary>
/// A diagnostic bag that stays empty.
/// </summary>
internal sealed class EmptyDiagnosticBag : DiagnosticBag
{
    /// <summary>
    /// A singleton instance of <see cref="EmptyDiagnosticBag"/>.
    /// </summary>
    public static EmptyDiagnosticBag Instance { get; } = new();

    public override int Count => 0;

    private EmptyDiagnosticBag()
    {
    }

    public override void Add(Diagnostic diagnostic) { }
    public override void Clear() { }
    public override IEnumerator<Diagnostic> GetEnumerator() => Enumerable.Empty<Diagnostic>().GetEnumerator();
}
