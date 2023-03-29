using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
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
        var sourceText = this.documentRepository.GetDocument(param.TextDocument.Uri);
        var tree = SyntaxTree.Parse(sourceText);
        var originalRange = tree.Root.Range;
        tree = tree.Format();
        var edit = new TextEdit()
        {
            NewText = tree.ToString(),
            Range = Translator.ToLsp(originalRange),
        };
        return Task.FromResult<IList<TextEdit>?>(new[] { edit });
    }
}
