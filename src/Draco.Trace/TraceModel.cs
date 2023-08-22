using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Draco.Trace;

internal sealed record class TraceModel(IReadOnlyList<ThreadTraceModel> Threads)
{
    public DateTime StartTime => this.Threads.Min(t => t.Messages[0].TimeStamp);
    public DateTime EndTime => this.Threads.Min(t => t.Messages[^1].TimeStamp);
    public TimeSpan TimeSpan => this.EndTime - this.StartTime;
}

internal sealed class ThreadTraceModel
{
    public Thread Thread { get; }
    public IReadOnlyList<MessageTraceModel> Messages { get; }

    internal TraceModel TraceModel { get; }

    public ThreadTraceModel(TraceModel traceModel, Thread thread, IReadOnlyList<MessageTraceModel> messages)
    {
        this.TraceModel = traceModel;
        this.Thread = thread;
        this.Messages = messages;
    }
}

internal sealed class MessageTraceModel
{
    public bool IsBeginEvent => this.UnderlyingMessage.Kind == TraceKind.Begin;
    public bool IsEndEvent => this.UnderlyingMessage.Kind == TraceKind.End;
    public bool IsInstantEvent => this.UnderlyingMessage.Kind == TraceKind.Event;

    public DateTime TimeStamp => this.UnderlyingMessage.TimeStamp;
    public string Message => this.UnderlyingMessage.Message;

    internal ThreadTraceModel ThreadTraceModel { get; }
    internal TraceMessage UnderlyingMessage { get; }

    public MessageTraceModel(ThreadTraceModel threadTraceModel, TraceMessage message)
    {
        this.ThreadTraceModel = threadTraceModel;
        this.UnderlyingMessage = message;
    }
}
