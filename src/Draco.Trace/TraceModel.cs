using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Draco.Trace;

internal sealed record class TraceModel(IReadOnlyList<ThreadTraceModel> Threads);

internal sealed class ThreadTraceModel
{
    public Thread Thread { get; }
    public IReadOnlyList<MessageTraceModel> Messages { get; }

    private readonly TraceModel traceModel;

    public ThreadTraceModel(TraceModel traceModel, Thread thread, IReadOnlyList<MessageTraceModel> messages)
    {
        this.traceModel = traceModel;
        this.Thread = thread;
        this.Messages = messages;
    }
}

internal sealed class MessageTraceModel
{
    public bool IsBeginEvent => this.message.Kind == TraceKind.Begin;
    public bool IsEndEvent => this.message.Kind == TraceKind.End;
    public bool IsInstantEvent => this.message.Kind == TraceKind.Event;

    public string Message => this.message.Message;

    private readonly ThreadTraceModel threadTraceModel;
    private readonly TraceMessage message;

    public MessageTraceModel(ThreadTraceModel threadTraceModel, TraceMessage message)
    {
        this.threadTraceModel = threadTraceModel;
        this.message = message;
    }
}
