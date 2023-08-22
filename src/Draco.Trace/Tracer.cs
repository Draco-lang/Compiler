using System;
using System.Collections.Concurrent;

namespace Draco.Trace;

public sealed class Tracer
{
    private readonly ConcurrentQueue<TraceMessage> messages = new();

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
