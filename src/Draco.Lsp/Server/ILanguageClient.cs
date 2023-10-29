using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Draco.JsonRpc;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server;

/// <summary>
/// An interface representing the language client on the remote.
/// </summary>
public interface ILanguageClient : IJsonRpcClient
{
    // Language features

    [Notification("textDocument/publishDiagnostics")]
    public Task PublishDiagnosticsAsync(PublishDiagnosticsParams param);

    // Workspace features

    [Request("workspace/configuration")]
    public Task<IList<JsonElement>> GetConfigurationAsync(ConfigurationParams param);

    // Window features

    [Notification("window/logMessage")]
    public Task LogMessageAsync(LogMessageParams param);
}
