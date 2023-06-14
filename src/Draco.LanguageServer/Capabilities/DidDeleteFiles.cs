using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Workspace;

namespace Draco.LanguageServer;

internal partial class DracoLanguageServer : IDidDeleteFiles
{
    public FileOperationRegistrationOptions DidDeleteFileRegistrationOptions => new()
    {
        Filters = new FileOperationFilter[]
        {
            new()
            {
                Pattern = new()
                {
                    Glob = this.DocumentSelector[0].Pattern!
                }
            }
        }
    };

    public async Task DidDeleteFilesAsync(DeleteFilesParams param, CancellationToken cancellationToken)
    {
        foreach (var file in param.Files)
        {
            await this.DeleteDocument(DocumentUri.From(file.Uri));
        }
    }

    private async Task DeleteDocument(DocumentUri documentUri)
    {
        var oldTree = this.GetSyntaxTree(documentUri);
        this.compilation = this.compilation.UpdateSyntaxTree(oldTree, null);
        await this.PublishDiagnosticsAsync(documentUri);
    }
}
