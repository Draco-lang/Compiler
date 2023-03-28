using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server;

/// <summary>
/// The interface that language servers need to implement.
/// </summary>
public interface ILanguageServer
{
    [Request("initialize")]
    public Task<InitializeResult> InitializeAsync(InitializeParams param);

    [Notification("initialized")]
    public Task InitializedAsync(InitializedParams param);
}
