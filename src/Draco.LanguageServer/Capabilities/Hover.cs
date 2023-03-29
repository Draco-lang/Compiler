using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;
using StreamJsonRpc;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : IHover
{
    public HoverRegistrationOptions HoverRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
    };

    public Task<Hover?> HoverAsync(HoverParams param, CancellationToken cancellationToken)
    {
        var cursorPosition = Translator.ToCompiler(param.Position);
        // TODO: Share compilation
        var souceText = this.documentRepository.GetDocument(param.TextDocument.Uri);
        var syntaxTree = SyntaxTree.Parse(souceText);
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(syntaxTree));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var referencedSymbol = syntaxTree
            .TraverseSubtreesAtPosition(cursorPosition)
            .Select(symbol => semanticModel.GetReferencedSymbol(symbol) ?? semanticModel.GetDefinedSymbol(symbol))
            .LastOrDefault(symbol => symbol is not null);

        var docs = referencedSymbol is null ? string.Empty : referencedSymbol.Documentation;

        return Task.FromResult<Hover?>(new Hover()
        {
            Contents = new MarkupContent()
            {
                Kind = MarkupKind.Markdown,
                Value = docs,
            },
        });
    }
}
