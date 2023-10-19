using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Workspace;

[ClientCapability("Workspace.FileOperations")]
public interface IDidDeleteFiles
{
    [RegistrationOptions("workspace/didDeleteFiles")]
    public FileOperationRegistrationOptions DidDeleteFileRegistrationOptions { get; }

    [Notification("workspace/didDeleteFiles", Mutating = true)]
    public Task DidDeleteFilesAsync(DeleteFilesParams param);
}
