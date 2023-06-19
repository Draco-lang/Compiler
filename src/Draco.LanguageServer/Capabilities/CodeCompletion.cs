using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : ICodeCompletion
{
    public CompletionRegistrationOptions CompletionRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
        TriggerCharacters = new[] { "." },
    };

    public Task<IList<CompletionItem>> CompleteAsync(CompletionParams param, CancellationToken cancellationToken)
    {
        var compilation = this.compilation;

        var syntaxTree = GetSyntaxTree(compilation, param.TextDocument.Uri);
        if (syntaxTree is null) return Task.FromResult<IList<CompletionItem>>(Array.Empty<CompletionItem>());

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var cursorPosition = Translator.ToCompiler(param.Position);
        var completionItems = this.completionService.GetCompletions(syntaxTree, semanticModel, cursorPosition);
        return Task.FromResult<IList<CompletionItem>>(completionItems.Select(Translator.ToLsp).ToList());
    }
}
