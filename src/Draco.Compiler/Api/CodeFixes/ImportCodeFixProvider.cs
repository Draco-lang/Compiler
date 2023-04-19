using System;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Api.CodeFixes;

public sealed class ImportCodeFixProvider : CodeFixProvider
{
    private SyntaxTree SyntaxTree { get; }
    private SyntaxRange Range { get; }

    public ImportCodeFixProvider(SyntaxTree tree, SyntaxRange range)
    {
        this.SyntaxTree = tree;
        this.Range = range;
    }

    internal override ImmutableArray<CodeFix> GetCodeFixes(ImmutableArray<Diagnostic> diagnostics)
    {
        if (this.SyntaxTree.TraverseSubtreesIntersectingRange(this.Range).LastOrDefault(x => x is ImportDeclarationSyntax) is ImportDeclarationSyntax import
            && diagnostics.Any(x => x.Template == SymbolResolutionErrors.ImportNotAtTop && x.Location.Range!.Value.Intersects(this.Range)))
        {
            return ImmutableArray.Create(
                new CodeFix("Move import statement to be at the top of a scope", this.TopOfScope()),
                new CodeFix("Move import statement  to be at the top of a file", this.TopOfFile()));
        }
        return ImmutableArray<CodeFix>.Empty;
    }

    private ImmutableArray<TextEdit> TopOfScope()
    {
        var import = this.SyntaxTree.TraverseSubtreesIntersectingRange(this.Range).LastOrDefault(x => x is ImportDeclarationSyntax);
        if (import is null) return ImmutableArray<TextEdit>.Empty;
        SyntaxTree newTree;
        if (import.Parent is DeclarationStatementSyntax) newTree = this.SyntaxTree.Reorder(import.Parent, 0);
        else newTree = this.SyntaxTree.Reorder(import, 0);
        return this.SyntaxTree.SyntaxTreeDiff(newTree);
    }

    private ImmutableArray<TextEdit> TopOfFile()
    {
        var import = this.SyntaxTree.TraverseSubtreesIntersectingRange(this.Range).LastOrDefault(x => x is ImportDeclarationSyntax);
        if (import is null) return ImmutableArray<TextEdit>.Empty;
        SyntaxTree newTree;
        if (import.Parent is DeclarationStatementSyntax) newTree = this.SyntaxTree.Remove(import.Parent);
        else newTree = this.SyntaxTree.Remove(import);
        newTree = newTree.Insert(import, newTree.Root, 0);
        return this.SyntaxTree.SyntaxTreeDiff(newTree);
    }
}
