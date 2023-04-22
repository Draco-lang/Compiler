using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Api.CodeFixes;

/// <summary>
/// Provides <see cref="CodeFix"/>es for import issues.
/// </summary>
public sealed class ImportCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> DiagnosticCodes { get; } = ImmutableArray.Create(SymbolResolutionErrors.ImportNotAtTop.Code);

    public override ImmutableArray<CodeFix> GetCodeFixes(Diagnostic diagnostic, SyntaxTree tree, SyntaxRange range)
    {
        // Checks if in the diagnostics is any diag this provider can fix, meaning it has the correct template and if it is in the range of this codefix
        if (tree.TraverseSubtreesIntersectingRange(range).LastOrDefault(x => x is ImportDeclarationSyntax) is ImportDeclarationSyntax import
            && diagnostic.Location.Range!.Value.Intersects(range))
        {
            return ImmutableArray.Create(
                new CodeFix("Move import statement to be at the top of a scope", this.TopOfScope(tree, range)),
                new CodeFix("Move import statement  to be at the top of a file", this.TopOfFile(tree, range)));
        }
        return ImmutableArray<CodeFix>.Empty;
    }

    private ImmutableArray<TextEdit> TopOfScope(SyntaxTree tree, SyntaxRange range)
    {
        var import = tree.TraverseSubtreesIntersectingRange(range).LastOrDefault(x => x is ImportDeclarationSyntax);
        if (import is null) return ImmutableArray<TextEdit>.Empty;
        SyntaxTree newTree;
        if (import.Parent is DeclarationStatementSyntax) newTree = tree.Reorder(import.Parent, 0);
        else newTree = tree.Reorder(import, 0);
        return tree.SyntaxTreeDiff(newTree);
    }

    private ImmutableArray<TextEdit> TopOfFile(SyntaxTree tree, SyntaxRange range)
    {
        var import = tree.TraverseSubtreesIntersectingRange(range).LastOrDefault(x => x is ImportDeclarationSyntax);
        if (import is null) return ImmutableArray<TextEdit>.Empty;
        var newTree = import.Parent is DeclarationStatementSyntax
            ? tree.Remove(import.Parent)
            : tree.Remove(import);
        newTree = newTree.Insert(import, newTree.Root, 0);
        return tree.SyntaxTreeDiff(newTree);
    }
}
