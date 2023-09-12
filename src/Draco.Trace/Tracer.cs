using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Draco.Trace.Model;

namespace Draco.Trace;

public sealed class Tracer
{
    /// <summary>
    /// A tracer that does not trace.
    /// </summary>
    public static Tracer Null { get; } = Create(isEnabled: false);

    /// <summary>
    /// Constructs a new <see cref="Tracer"/> for tracing events.
    /// </summary>
    /// <param name="isEnabled">True, if the tracer should be enabled.</param>
    /// <returns>The constructed <see cref="Tracer"/>.</returns>
    public static Tracer Create(bool isEnabled) => new(isEnabled);

    private static readonly Stopwatch stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// True, if this tracer is enabled.
    /// </summary>
    public bool IsEnabled { get; }

    private readonly ConcurrentQueue<TraceMessage> messages = new();

    private Tracer(bool isEnabled)
    {
        this.IsEnabled = isEnabled;
    }

    internal TraceModel ToScribanModel()
    {
        var result = new TraceModel();
        foreach (var group in this.messages.GroupBy(m => m.Thread))
        {
            var thread = new ThreadModel(result, group.Key);
            result.Threads.Add(thread);
            var parent = thread.Root;

            MessageModel AddMessage(string message, TimeSpan startTime)
            {
                var model = new MessageModel(thread!, parent)
                {
                    Message = message,
                    StartTime = startTime,
                    EndTime = startTime,
                };
                parent!.Children.Add(model);
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

            // Fix root parent data
            parent.Message = string.Empty;
            parent.StartTime = parent.Children[0].StartTime;
            parent.EndTime = parent.Children[^1].EndTime;
        }
        return result;
    }

    public void Event(string message) =>
        this.EnqueueMessage(CreateMessage(TraceKind.Event, message));

    public IDisposable Begin(string message)
    {
        this.EnqueueMessage(CreateMessage(TraceKind.Begin, message));
        return new TraceEnd(this);
    }

    internal void End() =>
        this.EnqueueMessage(CreateMessage(TraceKind.End));

    private void EnqueueMessage(TraceMessage message)
    {
        if (!this.IsEnabled) return;
        this.messages.Enqueue(message);
    }

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
        TimeStamp: stopwatch.Elapsed,
        Message: message ?? string.Empty);
}
