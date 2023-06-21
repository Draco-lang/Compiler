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
        var compilation = this.compilation;

        var syntaxTree = GetSyntaxTree(compilation, param.TextDocument.Uri);
        if (syntaxTree is null) return Task.FromResult(null as IList<TextEdit>);

        var originalRange = syntaxTree.Root.Range;
        syntaxTree = syntaxTree.Format();
        var edit = new TextEdit()
        {
            NewText = syntaxTree.ToString(),
            Range = Translator.ToLsp(originalRange),
        };
        return Task.FromResult<IList<TextEdit>?>(new[] { edit });
    }
}
