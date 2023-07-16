using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Workspace;

// TODO: Not correct, this should be an aggregate similar to ITextDocumentSync
[ClientCapability("Workspace.DidChangeConfiguration")]
public interface IConfiguration
{
    [RegistrationOptions("workspace/didChangeConfiguration")]
    public object DidOpenRegistrationOptions => new();

    // NOTE: https://github.com/microsoft/vscode-languageserver-node/issues/380
    // The config might not be here, use the notification as an opportunity to re-pull settings
    [Notification("workspace/didChangeConfiguration", Mutating = true)]
    public Task DidChangeConfigurationAsync(DidChangeConfigurationParams param);
}
