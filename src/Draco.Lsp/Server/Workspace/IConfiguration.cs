using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Workspace;

public interface IConfiguration
{
    [RegistrationOptions("workspace/didChangeConfiguration")]
    public object DidOpenRegistrationOptions => new();

    // NOTE: https://github.com/microsoft/vscode-languageserver-node/issues/380
    // The config might not be here, use the notification as an opportunity to re-pull settings
    [Notification("workspace/didChangeConfiguration")]
    public Task DidChangeConfigurationAsync(DidChangeConfigurationParams param);
}
