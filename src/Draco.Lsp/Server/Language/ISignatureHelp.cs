using Draco.Lsp.Attributes;
using System.Threading.Tasks;
using System.Threading;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Language;

public interface ISignatureHelp
{
    [Capability(nameof(ServerCapabilities.SignatureHelpProvider))]
    public SignatureHelpOptions? Capability => null;

    [RegistrationOptions("textDocument/signatureHelp")]
    public SignatureHelpRegistrationOptions SignatureHelpRegistrationOptions { get; }

    [Request("textDocument/signatureHelp")]
    public Task<SignatureHelp?> SignatureHelpAsync(SignatureHelpParams param, CancellationToken cancellationToken);
}
