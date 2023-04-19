using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : IFindReferences
{
    public ReferenceRegistrationOptions FindReferencesRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
    };

    public Task<IList<Location>> FindReferencesAsync(ReferenceParams param, CancellationToken cancellationToken)
    {
        var cursorPosition = Translator.ToCompiler(param.Position);

        var referencedSymbol = this.syntaxTree
            .TraverseSubtreesAtPosition(cursorPosition)
            .Select(symbol => this.semanticModel.GetReferencedSymbol(symbol) ?? this.semanticModel.GetDefinedSymbol(symbol))
            .LastOrDefault(symbol => symbol is not null);

        var references = new List<Location>();

        if (referencedSymbol is not null)
        {
            var referencingNodes = FindAllReferences(
                tree: this.syntaxTree,
                semanticModel: this.semanticModel,
                symbol: referencedSymbol,
                includeDeclaration: param.Context.IncludeDeclaration,
                cancellationToken: cancellationToken);
            foreach (var node in referencingNodes)
            {
                var location = Translator.ToLsp(node.Location);
                if (location is not null) references.Add(location);
            }
        }

        return Task.FromResult<IList<Location>>(references);
    }

    private static IEnumerable<SyntaxNode> FindAllReferences(
        SyntaxTree tree,
        SemanticModel semanticModel,
        ISymbol symbol,
        bool includeDeclaration,
        CancellationToken cancellationToken)
    {
        foreach (var node in tree.Root.PreOrderTraverse())
        {
            if (cancellationToken.IsCancellationRequested) yield break;

            if (symbol.Equals(semanticModel.GetReferencedSymbol(node)))
            {
                yield return node;
            }
            if (includeDeclaration && symbol.Equals(semanticModel.GetDefinedSymbol(node)))
            {
                yield return node;
            }
        }
    }
}
