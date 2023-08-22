using System;

namespace Draco.Trace;

internal sealed record class TraceEnd(Tracer Tracer, object EventId) : IDisposable
{
    public void Dispose() => this.Tracer.End(this.EventId);
}
