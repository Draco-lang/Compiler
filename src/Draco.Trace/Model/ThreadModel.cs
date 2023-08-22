using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Draco.Trace.Model;

internal sealed class ThreadModel
{
    public TraceModel Trace { get; }
    public Thread Thread { get; }
    public IList<MessageModel> Messages { get; } = new List<MessageModel>();

    public DateTime StartTime => this.Messages[0].StartTime;
    public DateTime EndTime => this.Messages[^1].EndTime;
    public TimeSpan TimeSpan => this.EndTime - this.StartTime;

    public ThreadModel(TraceModel trace, Thread thread)
    {
        this.Trace = trace;
        this.Thread = thread;
    }
}
