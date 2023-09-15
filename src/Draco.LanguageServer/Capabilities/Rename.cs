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

        // TODO: Consider adding an API that allows for right-inclusive subtree iteration
        var cursorRange = cursorPosition.Column == 0
            ? new SyntaxRange(cursorPosition, 1)
            : new SyntaxRange(new SyntaxPosition(Line: cursorPosition.Line, Column: cursorPosition.Column - 1), 1);
        var referencedSymbol = syntaxTree
            .TraverseSubtreesIntersectingRange(cursorRange)
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
            .GroupBy(n => n.Tree.SourceText.Path)
            .Select(g => (
                Path: g.Key,
                Edits: g.Select(n => RenameNode(n, param.NewName))))
            .Where(g => g.Path is not null);

        return Task.FromResult<WorkspaceEdit?>(new()
        {
            Changes = textEdits.ToDictionary(
                e => new DocumentUri(e.Path!.LocalPath),
                e => e.Edits.ToList() as IList<ITextEdit>),
        });
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

    private static ITextEdit RenameNode(SyntaxNode original, string name) => original switch
    {
        ParameterSyntax p => RenameToken(p.Name, name),
        GenericParameterSyntax g => RenameToken(g.Name, name),
        FunctionDeclarationSyntax f => RenameToken(f.Name, name),
        VariableDeclarationSyntax v => RenameToken(v.Name, name),
        LabelDeclarationSyntax l => RenameToken(l.Name, name),
        ModuleDeclarationSyntax m => RenameToken(m.Name, name),
        RootImportPathSyntax r => RenameToken(r.Name, name),
        MemberImportPathSyntax m => RenameToken(m.Member, name),
        MemberTypeSyntax m => RenameToken(m.Member, name),
        MemberExpressionSyntax m => RenameToken(m.Member, name),
        NameExpressionSyntax n => RenameToken(n.Name, name),
        NameTypeSyntax n => RenameToken(n.Name, name),
        NameLabelSyntax n => RenameToken(n.Name, name),
        SyntaxToken t when t.Parent is ForExpressionSyntax @for
                        && @for.Iterator == t => RenameToken(t, name),
        _ => throw new ArgumentOutOfRangeException(nameof(original)),
    };

    private static ITextEdit RenameToken(SyntaxToken token, string name) => new TextEdit()
    {
        Range = Translator.ToLsp(token.Range),
        NewText = name,
    };
}
