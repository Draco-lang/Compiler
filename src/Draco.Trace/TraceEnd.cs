using System;

namespace Draco.Trace;

internal sealed record class TraceEnd(Tracer Tracer) : IDisposable
{
    public void Dispose() => this.Tracer.End();
}
