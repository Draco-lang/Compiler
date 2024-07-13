using System.IO;
using Microsoft.Build.Framework;

namespace Draco.ProjectSystem;

internal sealed class StringWriterLogger(StringWriter writer) : ILogger
{
    public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Detailed;
    public string? Parameters { get => null; set { } }

    public void Initialize(IEventSource eventSource)
    {
        eventSource.AnyEventRaised += (sender, e) => writer.WriteLine(e.Message);
    }

    public void Shutdown() { }
}
