using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : ITextDocumentFormatting
{
    public DocumentFormattingRegistrationOptions DocumentFormattingRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
    };

    public Task<IList<TextEdit>?> FormatTextDocumentAsync(DocumentFormattingParams param, CancellationToken cancellationToken)
    {
        var originalRange = this.syntaxTree.Root.Range;
        this.syntaxTree = this.syntaxTree.Format();
        var edit = new TextEdit()
        {
            NewText = this.syntaxTree.ToString(),
            Range = Translator.ToLsp(originalRange),
        };
        return Task.FromResult<IList<TextEdit>?>(new[] { edit });
    }
}
