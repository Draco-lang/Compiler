using System.Threading.Tasks;
using Draco.LanguageServer.Configurations;
using Draco.Lsp.Model;
using Draco.Lsp.Server;

namespace Draco.LanguageServer;

internal sealed class DracoConfigurationRepository(ILanguageClient client)
{
    public InlayHintsConfiguration InlayHints { get; set; } = new();

    public async Task UpdateConfigurationAsync()
    {
        var cfg = await client.GetConfigurationAsync(new()
        {
            Items =
            [
                new ConfigurationItem()
                {
                    Section = "draco.inlayHints",
                }
            ],
        });

        this.InlayHints.ParameterNames = cfg[0].GetProperty("parameterNames"u8).GetBoolean();
        this.InlayHints.VariableTypes = cfg[0].GetProperty("variableTypes"u8).GetBoolean();
    }
}
