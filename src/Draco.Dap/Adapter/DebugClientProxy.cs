using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Draco.JsonRpc;

namespace Draco.Dap.Adapter;

// NOTE: Not sealed since DispatchProxy generates a class derived from this at runtime
internal class DebugClientProxy : JsonRpcClientProxy
{
    protected override IJsonRpcMethodHandler CreateHandler(MethodInfo method) =>
        new DebugAdapterMethodHandler(method, this);
}
