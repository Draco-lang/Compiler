using System.Reflection;
using Draco.JsonRpc;

namespace Draco.Lsp.Server;

// NOTE: Not sealed since DispatchProxy generates a class derived from this at runtime
internal class LanguageClientProxy : JsonRpcClientProxy
{
    protected override IJsonRpcMethodHandler CreateHandler(MethodInfo method) =>
        LanguageServerMethodHandler.Create(method, this);
}
