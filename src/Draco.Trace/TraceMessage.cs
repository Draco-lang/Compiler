using System;
using System.Threading;

namespace Draco.Trace;

internal readonly record struct TraceMessage(
    TraceKind Kind,
    Thread Thread,
    DateTime TimeStamp,
    string Message);
