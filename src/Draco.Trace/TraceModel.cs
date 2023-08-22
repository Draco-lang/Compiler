using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Draco.Trace;

internal sealed record class TraceModel(IReadOnlyList<ThreadTraceModel> Threads);

internal sealed record class ThreadTraceModel(Thread Thread, IReadOnlyList<TraceMessage> Messages);
