using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Draco.Trace.Model;

internal sealed class ThreadModel : ITimeSpanned
{
    public TraceModel Trace { get; }
    public Thread Thread { get; }
    public MessageModel Root { get; }

    public DateTime StartTime => this.Root.StartTime;
    public DateTime EndTime => this.Root.EndTime;
    public TimeSpan TimeSpan => this.EndTime - this.StartTime;

    public ThreadModel(TraceModel trace, Thread thread)
    {
        this.Trace = trace;
        this.Thread = thread;
        this.Root = new(this, null);
    }
}
