using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;
using StreamJsonRpc;

namespace Draco.Lsp.Server;

/// <summary>
/// An interface representing the language client on the remote.
/// </summary>
public interface ILanguageClient
{
    [Notification("window/logMessage")]
    public Task LogMessageAsync(LogMessageParams param);

    [Request("window/showMessageRequest")]
    public Task<MessageActionItem?> ShowMessageAsync(ShowMessageRequestParams param);
}
