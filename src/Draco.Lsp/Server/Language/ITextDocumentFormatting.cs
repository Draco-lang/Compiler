using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Language;

[ClientCapability("TextDocument.Formatting")]
public interface ITextDocumentFormatting
{
    [ServerCapability(nameof(ServerCapabilities.DocumentFormattingProvider))]
    public IDocumentFormattingOptions Capability => this.DocumentFormattingRegistrationOptions;

    [RegistrationOptions("textDocument/formatting")]
    public DocumentFormattingRegistrationOptions DocumentFormattingRegistrationOptions { get; }

    [Request("textDocument/formatting")]
    public Task<IList<TextEdit>?> FormatTextDocumentAsync(DocumentFormattingParams param, CancellationToken cancellationToken);
}
