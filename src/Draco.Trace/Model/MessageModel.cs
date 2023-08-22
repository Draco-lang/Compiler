using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Trace.Model;

internal sealed class MessageModel
{
    public TraceModel Trace => this.Thread.Trace;
    public ThreadModel Thread { get; }
    public MessageModel? Parent { get; }
    public string Message { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public IList<MessageModel> Children { get; } = new List<MessageModel>();

    public MessageModel(ThreadModel thread, MessageModel? parent)
    {
        this.Thread = thread;
        this.Parent = parent;
    }
}
