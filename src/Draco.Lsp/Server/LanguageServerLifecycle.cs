using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StreamJsonRpc;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server;

/// <summary>
/// Handles fundamental LSP lifecycle messages so the user does not have to.
/// </summary>
internal sealed class LanguageServerLifecycle
{
    private readonly ILanguageServer server;
    private readonly JsonRpc jsonRpc;

    public LanguageServerLifecycle(ILanguageServer server, JsonRpc jsonRpc)
    {
        this.server = server;
        this.jsonRpc = jsonRpc;
    }

    [JsonRpcMethod("initialize", UseSingleObjectParameterDeserialization = true)]
    public Task<InitializeResult> InitializeAsync(InitializedParams param)
    {
        // TODO: Collect capabilities
        return Task.FromResult(new InitializeResult()
        {
            ServerInfo = this.server.Info,
            Capabilities = new()
            {
                // TODO
            },
        });
    }

    [JsonRpcMethod("exit", UseSingleObjectParameterDeserialization = true)]
    public Task ExitAsync()
    {
        this.jsonRpc.Dispose();
        return Task.CompletedTask;
    }
}
