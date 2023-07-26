using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Language;

[ClientCapability("TextDocument.SignatureHelp")]
public interface ISignatureHelp
{
    [ServerCapability(nameof(ServerCapabilities.SignatureHelpProvider))]
    public ISignatureHelpOptions Capability => this.SignatureHelpRegistrationOptions;

    [RegistrationOptions("textDocument/signatureHelp")]
    public SignatureHelpRegistrationOptions SignatureHelpRegistrationOptions { get; }

    [Request("textDocument/signatureHelp")]
    public Task<SignatureHelp?> SignatureHelpAsync(SignatureHelpParams param, CancellationToken cancellationToken);
}
