using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Workspace;

public interface IDidDeleteFiles
{
    [RegistrationOptions("workspace/didDeleteFiles")]
    public FileOperationRegistrationOptions FileOperationRegistrationOptions { get; }

    [Request("workspace/didDeleteFiles")]
    public Task DidDeleteFilesAsync(DeleteFilesParams param, CancellationToken cancellationToken);
}
