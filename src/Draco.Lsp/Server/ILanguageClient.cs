using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;
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

    [Notification("textDocument/publishDiagnostics")]
    public Task PublishDiagnosticsAsync(PublishDiagnosticsParams param);

    [Notification("window/logMessage")]
    public Task LogMessageAsync(LogMessageParams param);
}
