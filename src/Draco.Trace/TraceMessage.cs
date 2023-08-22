using System;

namespace Draco.Trace;

internal readonly record struct TraceMessage(
    TraceKind Kind,
    DateTime TimeStamp,
    object EventId,
    string Message);
