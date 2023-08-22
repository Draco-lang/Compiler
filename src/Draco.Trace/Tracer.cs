using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Draco.Trace;

public sealed class Tracer
{
    private readonly ConcurrentQueue<TraceMessage> messages = new();

    internal TraceModel ToScribanModel()
    {
        var threadModels = new List<ThreadTraceModel>();
        var result = new TraceModel(threadModels);

        foreach (var group in this.messages.GroupBy(m => m.Thread))
        {
            var messages = new List<MessageTraceModel>();
            var threadModel = new ThreadTraceModel(result, group.Key, messages);
            threadModels.Add(threadModel);

            foreach (var message in group.OrderBy(m => m.TimeStamp))
            {
                messages.Add(new MessageTraceModel(threadModel, message));
            }

        }

        return result;
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

    public void RenderTimeline(Stream stream, CancellationToken cancellationToken)
    {
        var writer = new StreamWriter(stream);
        writer.Write(ScribanRenderer.Render("TimelineChart.sbncs", this.ToScribanModel(), cancellationToken));
        writer.Flush();
    }

    private static TraceMessage CreateMessage(
        TraceKind kind,
        string? message = null) => new(
        Kind: kind,
        Thread: Thread.CurrentThread,
        TimeStamp: DateTime.Now,
        Message: message ?? string.Empty);
}
