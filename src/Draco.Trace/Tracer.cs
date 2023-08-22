using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Draco.Trace;

public sealed class Tracer
{
    private readonly ConcurrentQueue<TraceMessage> messages = new();

    // TODO: Temporary
    public void Dump()
    {
        var l = this.messages.ToList();
        var indent = string.Empty;
        foreach (var m in l)
        {
            if (m.Kind == TraceKind.Begin)
            {
                Console.WriteLine($"{indent}{m.Message}");
                indent = $"{indent}  ";
            }
            else if (m.Kind == TraceKind.End)
            {
                indent = indent[..^2];
            }
            else
            {
                Console.WriteLine($"{indent}{m.Message}");
            }
        }
    }

    public void Event(string message) =>
        this.messages.Enqueue(CreateMessage(TraceKind.Event, message));

    public IDisposable Begin(string message)
    {
        this.messages.Enqueue(CreateMessage(TraceKind.Begin, message));
        return new TraceEnd(this);
    }

    internal void End() =>
        this.messages.Enqueue(CreateMessage(TraceKind.End));

    private static TraceMessage CreateMessage(
        TraceKind kind,
        string? message = null) => new(
        Kind: kind,
        TimeStamp: DateTime.Now,
        Message: message ?? string.Empty);
}
