using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server;

internal interface ILanguageServerLifecycle
{
    [Request("initialize")]
    public Task<InitializeResult> InitializeAsync(InitializeParams param);

    [Notification("initialized")]
    public Task InitializedAsync(InitializedParams param);

    [Notification("exit")]
    public Task ExitAsync();
}
