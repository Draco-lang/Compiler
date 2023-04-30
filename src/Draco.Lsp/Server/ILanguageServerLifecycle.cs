using Draco.Lsp.Attributes;
using Draco.Lsp.Model;
using System.Threading.Tasks;

namespace Draco.Lsp.Server;

public interface ILanguageServerLifecycle
{
    [Request("initialize")]
    public Task<InitializeResult> InitializeAsync(InitializedParams param);

    [Notification("initialized")]
    public Task InitializedAsync(InitializedParams param);

    [Notification("exit")]
    public Task ExitAsync();
}
