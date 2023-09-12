using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Trace.Model;

internal sealed class MessageModel : ITimeSpanned
{
    public TraceModel Trace => this.Thread.Trace;
    public ThreadModel Thread { get; }
    public MessageModel? Parent { get; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public ImmutableArray<object?> Parameters { get; set; }
    public object? Result { get; set; }
    public IList<MessageModel> Children { get; } = new List<MessageModel>();

    public int Height => (this.Parent?.Height ?? 0) + 1;

    public TimeSpan TimeSpan => this.EndTime - this.StartTime;

    public double AbsoluteSpanPercentage => this.TimeSpan.TotalSeconds / this.Trace.TimeSpan.TotalSeconds;
    public double AbsoluteStartPercentage => (this.StartTime - this.Trace.StartTime).TotalSeconds / this.Trace.TimeSpan.TotalSeconds;
    public double AbsoluteEndPercentage => (this.EndTime - this.Trace.StartTime).TotalSeconds / this.Trace.TimeSpan.TotalSeconds;

    public double RelativeSpanPercentage => this.TimeSpan.TotalSeconds / this.Enclosing.TimeSpan.TotalSeconds;
    public double RelativeStartPercentage => (this.StartTime - this.Enclosing.StartTime).TotalSeconds / this.Enclosing.TimeSpan.TotalSeconds;
    public double RelativeEndPercentage => (this.EndTime - this.Enclosing.StartTime).TotalSeconds / this.Enclosing.TimeSpan.TotalSeconds;

    public double TimeSpanInMilliseconds => this.TimeSpan.TotalMilliseconds;

    private ITimeSpanned Enclosing => this.Parent as ITimeSpanned ?? this.Thread;

    public MessageModel(ThreadModel thread, MessageModel? parent)
    {
        this.Thread = thread;
        this.Parent = parent;
    }
}
