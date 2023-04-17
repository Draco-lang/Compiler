using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;

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

        var referencedSymbol = this.syntaxTree
            .TraverseSubtreesAtPosition(cursorPosition)
            .Select(symbol => this.semanticModel.GetReferencedSymbol(symbol) ?? this.semanticModel.GetDeclaredSymbol(symbol))
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
