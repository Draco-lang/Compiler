using System.Collections.Immutable;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;

namespace Draco.Compiler.Internal.Semantics.DFA;

internal sealed record class CFG
{
    // TODO: add builder
    /// <summary>
    /// Represents a single block in <see cref="CFG"/> consisting non-branching statements, the block ends branching.
    /// </summary>
    /// <param name="Statements">List of statements contained in the <see cref="Block"/>.</param>
    /// <param name="Branches">List of possible <see cref="Branch"/>es.</param>
    internal sealed record class Block(ImmutableArray<Ast.Stmt> Statements, ImmutableArray<Branch> Branches);

    /// <summary>
    /// Represents single <see cref="Branch"/>.
    /// </summary>
    /// <param name="Target">The <see cref="Block"/> targeted by this <see cref="Branch"/>.</param>
    /// <param name="Condition">The condition that must be met so the execution continues to the <paramref name="Target"/>.</param>
    internal sealed record class Branch(Block Target, Ast.Expr Condition);

    internal sealed record class Builder
    {
        private Stack
    }
}
