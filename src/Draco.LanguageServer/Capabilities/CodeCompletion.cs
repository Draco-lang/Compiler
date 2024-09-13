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
        TriggerCharacters = ["."],
    };

    public Task<IList<CompletionItem>> CompleteAsync(CompletionParams param, CancellationToken cancellationToken)
    {
        var compilation = this.compilation;

        var syntaxTree = GetSyntaxTree(compilation, param.TextDocument.Uri);
        if (syntaxTree is null) return Task.FromResult<IList<CompletionItem>>(Array.Empty<CompletionItem>());

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var cursorPosition = Translator.ToCompiler(param.Position);
        var cursorIndex = syntaxTree.SyntaxPositionToIndex(cursorPosition);
        var completionItems = this.completionService.GetCompletions(semanticModel, cursorIndex);
        var translatedCompletionItems = completionItems
            .Select(i => Translator.ToLsp(syntaxTree.SourceText, i))
            .ToList();
        return Task.FromResult<IList<CompletionItem>>(translatedCompletionItems);
    }
}
