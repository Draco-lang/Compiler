using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Draco.JsonRpc;

namespace Draco.Lsp.Server;

// NOTE: Not sealed since DispatchProxy generates a class derived from this at runtime
internal class LanguageClientProxy : JsonRpcClientProxy
{
    protected override IJsonRpcMethodHandler CreateHandler(MethodInfo method) =>
        LanguageServerMethodHandler.Create(method, this);
}
