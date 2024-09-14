using Draco.Compiler.Internal.Syntax.Formatting;
using Draco.Compiler.Internal.Syntax.Rewriting;
using System.Collections.Immutable;

namespace Draco.Compiler.Api.Syntax.Extensions;

/// <summary>
/// Extensions for rewriting syntax trees.
/// </summary>
public static class SyntaxTreeRewriteExtensions
{
    // TODO: Doc, rewrite...
    public static SyntaxTree Reorder(this SyntaxTree tree, SyntaxNode toReorder, int position) => new(
        tree.SourceText,
        tree.GreenRoot.Accept(new ReorderRewriter(toReorder.Green, position)),
        new());

    // TODO: Doc, rewrite...
    public static SyntaxTree Remove(this SyntaxTree tree, SyntaxNode toRemove) => new(
        tree.SourceText,
        tree.GreenRoot.Accept(new RemoveRewriter(toRemove.Green)),
        new());

    // TODO: Doc, rewrite...
    public static SyntaxTree Insert(this SyntaxTree tree, SyntaxNode toInsert, SyntaxNode insertInto, int position) => new(
        tree.SourceText,
        tree.GreenRoot.Accept(new InsertRewriter(toInsert.Green, insertInto.Green, position)),
        new());

    // TODO: Doc, rewrite...
    public static ImmutableArray<TextEdit> SyntaxTreeDiff(this SyntaxTree tree, SyntaxTree other) =>
        // TODO: We can use a better diff algo
        [new TextEdit(tree.Root.Span, other.ToString())];

    // TODO: Doc, rewrite...
    public static SyntaxTree Format(this SyntaxTree tree) => Formatter.Format(tree);
}
