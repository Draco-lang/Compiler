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

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : IInlayHint
{
    public InlayHintRegistrationOptions InlayHintRegistrationOptions => new()
    {
        DocumentSelector = DocumentSelector,
    };

    public Task<IList<InlayHint>> InlayHintAsync(InlayHintParams param, CancellationToken cancellationToken)
    {
        var range = Translator.ToCompiler(param.Range);
        // TODO: Share compilation
        var souceText = this.documentRepository.GetDocument(param.TextDocument.Uri);
        var syntaxTree = SyntaxTree.Parse(souceText);
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(syntaxTree));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var inlayHints = new List<InlayHint>();

        // TODO: Add inlay hints

        return Task.FromResult<IList<InlayHint>>(inlayHints);
    }
}
