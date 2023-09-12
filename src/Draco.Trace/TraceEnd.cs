using System;

namespace Draco.Trace;

public sealed class TraceEnd : IDisposable
{
    public static TraceEnd Null { get; } = new(Tracer.Null);

    public object? Result { get; set; }

    private readonly Tracer tracer;

    internal TraceEnd(Tracer tracer)
    {
        this.tracer = tracer;
    }

    public void Dispose() => this.tracer.End(result: this.Result);
}
