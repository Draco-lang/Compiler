using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        var compilation = this.compilation;

        var syntaxTree = GetSyntaxTree(compilation, param.TextDocument.Uri);
        if (syntaxTree is null) return Task.FromResult<IList<Location>>(Array.Empty<Location>());

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var cursorPosition = Translator.ToCompiler(param.Position);

        var referencedSymbol = syntaxTree
            .TraverseSubtreesAtPosition(cursorPosition)
            .Select(symbol => semanticModel.GetReferencedSymbol(symbol) ?? semanticModel.GetDeclaredSymbol(symbol))
            .LastOrDefault(symbol => symbol is not null);

        var references = new List<Location>();

        if (referencedSymbol is not null)
        {
            var referencingNodes = FindAllReferences(
                trees: compilation.SyntaxTrees,
                semanticModel: semanticModel,
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
        ImmutableArray<SyntaxTree> trees,
        SemanticModel semanticModel,
        ISymbol symbol,
        bool includeDeclaration,
        CancellationToken cancellationToken)
    {
        foreach (var tree in trees)
        {
            foreach (var node in tree.Root.PreOrderTraverse())
            {
                if (cancellationToken.IsCancellationRequested) yield break;

                if (symbol.Equals(semanticModel.GetReferencedSymbol(node)))
                {
                    yield return node;
                }
                if (includeDeclaration && symbol.Equals(semanticModel.GetDeclaredSymbol(node)))
                {
                    yield return node;
                }
            }
        }
    }
}
