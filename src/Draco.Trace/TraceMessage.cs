using System;
using System.Collections.Immutable;
using System.Threading;

namespace Draco.Trace;

internal readonly record struct TraceMessage(
    TraceKind Kind,
    Thread Thread,
    TimeSpan TimeStamp,
    string Message,
    ImmutableArray<object?> Parameters,
    object? Result);
