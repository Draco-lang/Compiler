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
    public override ImmutableArray<string> DiagnosticCodes { get; } = [SymbolResolutionErrors.ImportNotAtTop.Code];

    public override ImmutableArray<CodeFix> GetCodeFixes(Diagnostic diagnostic, SyntaxTree tree, SyntaxRange range)
    {
        // Checks if in the diagnostics is any diag this provider can fix, meaning it has the correct template and if it is in the range of this codefix
        if (tree.TraverseSubtreesIntersectingRange(range).LastOrDefault(x => x is ImportDeclarationSyntax) is ImportDeclarationSyntax import
            && diagnostic.Location.Range!.Value.Intersects(range))
        {
            return
            [
                new CodeFix("Move import statement to be at the top of the scope", this.TopOfScope(tree, range)),
                new CodeFix("Move import statement to be at the top of the file", this.TopOfFile(tree, range)),
            ];
        }
        return [];
    }

    private ImmutableArray<TextEdit> TopOfScope(SyntaxTree tree, SyntaxRange range)
    {
        var import = tree.TraverseSubtreesIntersectingRange(range).LastOrDefault(x => x is ImportDeclarationSyntax);
        if (import is null) return [];
        var newTree = import.Parent is DeclarationStatementSyntax
            ? tree.Reorder(import.Parent, 0)
            : tree.Reorder(import, 0);
        return tree.SyntaxTreeDiff(newTree);
    }

    private ImmutableArray<TextEdit> TopOfFile(SyntaxTree tree, SyntaxRange range)
    {
        var import = tree.TraverseSubtreesIntersectingRange(range).LastOrDefault(x => x is ImportDeclarationSyntax);
        if (import is null) return [];
        var newTree = import.Parent is DeclarationStatementSyntax
            ? tree.Remove(import.Parent)
            : tree.Remove(import);
        newTree = newTree.Insert(import, newTree.Root, 0);
        return tree.SyntaxTreeDiff(newTree);
    }
}
