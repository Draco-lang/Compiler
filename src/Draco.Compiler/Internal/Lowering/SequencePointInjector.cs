using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Internal.Lowering;

/// <summary>
/// Injects sequence points into the bound tree.
/// </summary>
internal sealed class SequencePointInjector : BoundTreeRewriter
{
    /// <summary>
    /// The singleton instance to use.
    /// </summary>
    public static SequencePointInjector Instance { get; } = new();

    private SequencePointInjector()
    {
    }

    public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
    {
        var injected = (BoundStatement)base.VisitLocalDeclaration(node);
        return SequencePointStatement(
            statement: injected,
            range: null,
            // If the value is null, there is nothing to compile
            // So we enforce a NOP to be emitted
            emitNop: node.Value is null);
    }

    public override BoundNode VisitBlockExpression(BoundBlockExpression node)
    {
        if (node.Syntax is null) return base.VisitBlockExpression(node);

        // We add sequence points to the braces
        // {
        // ^ here
        //     ...
        // }
        // ^ and here
        //
        // For that to function correctly, we need to wrap the original block to add pseudo-statements
        // {
        //     <sequence point for {>
        //     val blockValue = { original block contents... };
        //     <sequence point for }>
        //     blockValue
        // }

        var openBrace = node.Syntax.Children.First();
        var closeBrace = node.Syntax.Children.Last();

        var injectedBlock = (BoundExpression)base.VisitBlockExpression(node);
        var blockValue = new SynthetizedLocalSymbol(node.Type, false);

        return BlockExpression(
            locals: ImmutableArray.Create<LocalSymbol>(blockValue),
            statements: ImmutableArray.Create<BoundStatement>(
                SequencePointStatement(
                    statement: null,
                    range: openBrace.Range,
                    emitNop: true),
                LocalDeclaration(blockValue, injectedBlock)
                // TODO: Fix and readd this
                /*SequencePointStatement(
                    statement: null,
                    range: closeBrace.Range,
                    emitNop: true)*/
                ),
            value: LocalExpression(blockValue));
    }
}
