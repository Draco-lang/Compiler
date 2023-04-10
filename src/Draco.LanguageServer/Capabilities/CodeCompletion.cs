using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CompletionService = Draco.Compiler.Api.CodeCompletion.CompletionService;
using Draco.Compiler.Api.Syntax;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : ICodeCompletion
{
    public CompletionRegistrationOptions CompletionRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
    };

    public Task<IList<CompletionItem>> CompleteAsync(CompletionParams param, CancellationToken cancellationToken)
    {
        var cursorPosition = Translator.ToCompiler(param.Position);
        var souceText = this.documentRepository.GetDocument(param.TextDocument.Uri);
        var syntaxTree = SyntaxTree.Parse(souceText);
        var completionItems = new List<CompletionItem>();
        return Task.FromResult<IList<CompletionItem>>(CompletionService.GetCompletions(syntaxTree, cursorPosition).Select(x => Translator.ToLsp(x)).ToList());
    }
}
