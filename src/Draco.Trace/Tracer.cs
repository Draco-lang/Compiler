using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Draco.Trace.Model;

namespace Draco.Trace;

public sealed class Tracer
{
    private readonly ConcurrentQueue<TraceMessage> messages = new();

    internal TraceModel ToScribanModel()
    {
        var result = new TraceModel();
        foreach (var group in this.messages.GroupBy(m => m.Thread))
        {
            var thread = new ThreadModel(result, group.Key);
            result.Threads.Add(thread);
            var parent = null as MessageModel;

            MessageModel AddMessage(string message, DateTime startTime)
            {
                var model = new MessageModel(thread!, parent)
                {
                    StartTime = startTime,
                    EndTime = startTime,
                };
                if (parent is null) thread!.Messages.Add(model);
                else parent.Children.Add(model);
                return model;
            }

            foreach (var message in group.OrderBy(m => m.TimeStamp))
            {
                switch (message.Kind)
                {
                case TraceKind.Event:
                    AddMessage(message.Message, message.TimeStamp);
                    break;
                case TraceKind.Begin:
                    parent = AddMessage(message.Message, message.TimeStamp);
                    break;
                case TraceKind.End:
                    parent!.EndTime = message.TimeStamp;
                    parent = parent.Parent;
                    break;
                default:
                    throw new InvalidOperationException();
                }
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
