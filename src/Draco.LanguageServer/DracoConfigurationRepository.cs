using System.Threading.Tasks;
using Draco.LanguageServer.Configurations;
using Draco.Lsp.Model;
using Draco.Lsp.Server;

namespace Draco.LanguageServer;

internal sealed class DracoConfigurationRepository
{
    public InlayHintsConfiguration InlayHints { get; set; } = new();

    private readonly ILanguageClient client;

    public DracoConfigurationRepository(ILanguageClient client)
    {
        this.client = client;
    }

    public async Task UpdateConfigurationAsync()
    {
        var cfg = await this.client.GetConfigurationAsync(new()
        {
            Items = new[]
            {
                new ConfigurationItem()
                {
                    Section = "draco.inlayHints",
                }
            },
        });

        this.InlayHints.ParameterNames = cfg[0].GetProperty("parameterNames"u8).GetBoolean();
        this.InlayHints.VariableTypes = cfg[0].GetProperty("variableTypes"u8).GetBoolean();
    }
}
