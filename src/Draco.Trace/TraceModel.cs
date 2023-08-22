using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Draco.Trace;

internal sealed record class TraceModel(IReadOnlyList<ThreadTraceModel> Threads);

internal sealed record class ThreadTraceModel(Thread Thread, IReadOnlyList<MessageTraceModel> Messages);

internal sealed class MessageTraceModel
{
    public bool IsBeginEvent => this.message.Kind == TraceKind.Begin;
    public bool IsEndEvent => this.message.Kind == TraceKind.End;
    public bool IsInstantEvent => this.message.Kind == TraceKind.Event;

    public string Message => this.message.Message;

    private readonly TraceMessage message;

    public MessageTraceModel(TraceMessage message)
    {
        this.message = message;
    }
}
