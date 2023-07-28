using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Workspace;

[ClientCapability("Workspace.DidChangeConfiguration")]
public interface IDidChangeConfiguration
{
    [RegistrationOptions("workspace/didChangeConfiguration")]
    public object DidChangeConfigurationRegistrationOptions => new();

    // NOTE: https://github.com/microsoft/vscode-languageserver-node/issues/380
    // The config might not be here, use the notification as an opportunity to re-pull settings
    [Notification("workspace/didChangeConfiguration", Mutating = true)]
    public Task DidChangeConfigurationAsync(DidChangeConfigurationParams param);
}
