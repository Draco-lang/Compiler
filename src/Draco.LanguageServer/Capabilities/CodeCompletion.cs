using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CompletionService = Draco.Compiler.Api.CodeCompletion.CompletionService;
using KeywordCompletionProvider = Draco.Compiler.Api.CodeCompletion.KeywordCompletionProvider;
using SemanticCompletionProvider = Draco.Compiler.Api.CodeCompletion.SemanticCompletionProvider;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : ICodeCompletion
{
    public CompletionRegistrationOptions CompletionRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
        TriggerCharacters = new[] { "." }
    };

    public Task<IList<CompletionItem>> CompleteAsync(CompletionParams param, CancellationToken cancellationToken)
    {
        var cursorPosition = Translator.ToCompiler(param.Position);
        var service = new CompletionService();
        service.AddProvider(new KeywordCompletionProvider());
        service.AddProvider(new SemanticCompletionProvider());
        return Task.FromResult<IList<CompletionItem>>(service.GetCompletions(this.syntaxTree, this.semanticModel, cursorPosition).Select(x => Translator.ToLsp(x)).ToList());
    }
}
