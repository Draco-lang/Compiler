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

internal partial class DracoLanguageServer : IRename
{
    public RenameRegistrationOptions RenameRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
        PrepareProvider = false,
    };

    public Task<WorkspaceEdit?> RenameAsync(RenameParams param, CancellationToken cancellationToken)
    {
        var compilation = this.compilation;

        var syntaxTree = GetSyntaxTree(compilation, param.TextDocument.Uri);
        if (syntaxTree is null) return Task.FromResult<WorkspaceEdit?>(null);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var cursorPosition = Translator.ToCompiler(param.Position);

        var referencedSymbol = syntaxTree
            .TraverseSubtreesAtPosition(cursorPosition)
            .Select(symbol => semanticModel.GetReferencedSymbol(symbol) ?? semanticModel.GetDeclaredSymbol(symbol))
            .LastOrDefault(symbol => symbol is not null);
        if (referencedSymbol is null) return Task.FromResult<WorkspaceEdit?>(null);

        // TODO: Check if symbol is owned by this compilation

        var referencedNodes = FindAllAppearances(
            trees: compilation.SyntaxTrees,
            semanticModel: semanticModel,
            symbol: referencedSymbol,
            cancellationToken: cancellationToken);
        var textEdits = referencedNodes
            .Select(n => MakeTextEdit(n, param.NewName));

        // TODO

        throw new NotImplementedException();
    }

    private static IEnumerable<SyntaxNode> FindAllAppearances(
        ImmutableArray<SyntaxTree> trees,
        SemanticModel semanticModel,
        ISymbol symbol,
        CancellationToken cancellationToken)
    {
        foreach (var tree in trees)
        {
            foreach (var node in tree.Root.PreOrderTraverse())
            {
                if (cancellationToken.IsCancellationRequested) yield break;

                var referencedSymbol = semanticModel.GetReferencedSymbol(node)
                                    ?? semanticModel.GetDeclaredSymbol(node);
                if (referencedSymbol is null) continue;

                if (symbol.Equals(referencedSymbol)) yield return node;
            }
        }
    }

    private static ITextEdit? MakeTextEdit(SyntaxNode original, string name) => original switch
    {
        _ => throw new ArgumentOutOfRangeException(nameof(original)),
    };
}
