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
public interface ILanguageServer : IDisposable
{
    /// <summary>
    /// General server information.
    /// </summary>
    public InitializeResult.ServerInfoResult? Info { get; }

    [Notification("initialized")]
    public Task InitializedAsync(InitializedParams param);

    [Request("shutdown")]
    public Task ShutdownAsync();
}
