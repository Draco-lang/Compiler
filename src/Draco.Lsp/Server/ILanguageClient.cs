using System.Collections.Generic;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Draco.Lsp.Server;

/// <summary>
/// An interface representing the language client on the remote.
/// </summary>
public interface ILanguageClient
{
    /// <summary>
    /// The RPC connection between the client and the server.
    /// </summary>
    public JsonRpc Connection { get; }

    // Language features

    [Notification("textDocument/publishDiagnostics")]
    public Task PublishDiagnosticsAsync(PublishDiagnosticsParams param);

    // Workspace features

    [Request("workspace/configuration")]
    public Task<IList<JToken>> GetConfigurationAsync(ConfigurationParams param);

    // Window features

    [Notification("window/logMessage")]
    public Task LogMessageAsync(LogMessageParams param);
}
