using System.Reflection;
using Draco.JsonRpc;

namespace Draco.Dap.Adapter;

// NOTE: Not sealed since DispatchProxy generates a class derived from this at runtime
internal class DebugClientProxy : JsonRpcClientProxy
{
    protected override IJsonRpcMethodHandler CreateHandler(MethodInfo method) =>
        DebugAdapterMethodHandler.Create(method, this);
}
