using System.Threading.Tasks;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Workspace;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : IDidChangeConfiguration
{
    public async Task DidChangeConfigurationAsync(DidChangeConfigurationParams param)
    {
        await this.configurationRepository.UpdateConfigurationAsync();
    }
}
