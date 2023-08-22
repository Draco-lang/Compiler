using System;

namespace Draco.Trace;

internal readonly record struct TraceMessage(
    TraceKind Kind,
    DateTime TimeStamp,
    string Message);
