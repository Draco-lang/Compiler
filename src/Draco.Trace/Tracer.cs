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

    public void Event(string message) => this.Event(message, message);

    public void Event(object eventId, string message) =>
        this.messages.Enqueue(CreateMessage(TraceKind.Event, eventId, message));

    public IDisposable Begin(string message) => this.Begin(message, message);

    public IDisposable Begin(object eventId, string message)
    {
        this.messages.Enqueue(CreateMessage(TraceKind.Begin, eventId, message));
        return new TraceEnd(this, eventId);
    }

    internal void End(object eventId) =>
        this.messages.Enqueue(CreateMessage(TraceKind.End, eventId));

    private static TraceMessage CreateMessage(
        TraceKind kind,
        object eventId,
        string? message = null) => new(
        Kind: kind,
        TimeStamp: DateTime.Now,
        EventId: eventId,
        Message: message ?? string.Empty);
}
