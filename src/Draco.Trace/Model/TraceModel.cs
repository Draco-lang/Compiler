using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Trace.Model;

internal sealed class TraceModel : ITimeSpanned
{
    public IList<ThreadModel> Threads { get; } = new List<ThreadModel>();

    public DateTime StartTime => this.Threads.Min(t => t.StartTime);
    public DateTime EndTime => this.Threads.Max(t => t.EndTime);
    public TimeSpan TimeSpan => this.EndTime - this.StartTime;
}
