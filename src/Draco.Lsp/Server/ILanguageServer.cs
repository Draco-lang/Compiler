using System;
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

    // NOTE: This is handled by the lifecycle manager, so it's not annotated
    // The lifecycle manager will dynamically register capabilities here,
    // then invokes this method
    public Task InitializedAsync(InitializedParams param);

    [Request("shutdown")]
    public Task ShutdownAsync();
}
