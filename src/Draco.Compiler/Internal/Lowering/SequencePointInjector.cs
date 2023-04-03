using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
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

    public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
    {
        var injected = (BoundStatement)base.VisitExpressionStatement(node);

        if (!IsCompoundExpression(node.Expression))
        {
            // Wrap in a sequence point
            injected = SequencePointStatement(
                statement: injected,
                range: node.Syntax?.Range,
                emitNop: false);
        }

        return injected;
    }

    public override BoundNode VisitBlockExpression(BoundBlockExpression node)
    {
        if (node.Syntax is null) return base.VisitBlockExpression(node);

        // Check braces too, as we desugar many things to blocks
        var openBrace = node.Syntax.Children.First();
        var closeBrace = node.Syntax.Children.Last();
        if (openBrace is not SyntaxToken { Kind: TokenKind.CurlyOpen }) return base.VisitBlockExpression(node);
        if (closeBrace is not SyntaxToken { Kind: TokenKind.CurlyClose }) return base.VisitBlockExpression(node);

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

        var injectedBlock = (BoundExpression)base.VisitBlockExpression(node);
        var blockValue = new SynthetizedLocalSymbol(node.Type, false);

        return BlockExpression(
            locals: ImmutableArray.Create<LocalSymbol>(blockValue),
            statements: ImmutableArray.Create<BoundStatement>(
                // TODO: Fix and readd this
                SequencePointStatement(
                    statement: null,
                    range: openBrace.Range,
                    emitNop: true),
                LocalDeclaration(blockValue, injectedBlock),
                // TODO: Fix and readd this
                SequencePointStatement(
                    statement: null,
                    range: closeBrace.Range,
                    emitNop: true)
                ),
            value: LocalExpression(blockValue));
    }

    public override BoundNode VisitIfExpression(BoundIfExpression node)
    {
        // We wrap the condition
        var condition = (BoundExpression)node.Condition.Accept(this);
        condition = SequencePointExpression(
            expression: condition,
            range: GetParenthesizedRange(node.Condition.Syntax),
            emitNop: true);
        // Leave branches as-is
        var then = (BoundExpression)node.Then.Accept(this);
        var @else = (BoundExpression)node.Else.Accept(this);

        return IfExpression(
            condition: condition,
            then: then,
            @else: @else,
            type: node.Type);
    }

    public override BoundNode VisitWhileExpression(BoundWhileExpression node)
    {
        // We wrap the condition
        var condition = (BoundExpression)node.Condition.Accept(this);
        condition = SequencePointExpression(
            expression: condition,
            range: GetParenthesizedRange(node.Condition.Syntax),
            emitNop: true);
        // Leave body as-is
        var then = (BoundExpression)node.Then.Accept(this);

        return WhileExpression(
            condition: condition,
            then: then,
            continueLabel: node.ContinueLabel,
            breakLabel: node.BreakLabel);
    }

    // TODO: We'll need this for more sophisticated return expr
    private static SyntaxNode? GetBlockFunctionBodyAncestor(SyntaxNode? syntax)
    {
        while (true)
        {
            if (syntax is null) return null;
            switch (syntax)
            {
            case BlockFunctionBodySyntax block:
                return block;
            case InlineFunctionBodySyntax:
            case UnexpectedFunctionBodySyntax:
                return null;
            default:
                syntax = syntax.Parent;
                break;
            }
        }
    }

    /// <summary>
    /// Searches for a parenthesized range for a given syntax element. This is used for
    /// if-else and loop conditions.
    /// </summary>
    /// <param name="syntax">The syntax to retrieve the parenthesized range of (probably a condition).</param>
    /// <returns>The range of the parenthesized range.</returns>
    private static SyntaxRange? GetParenthesizedRange(SyntaxNode? syntax)
    {
        if (syntax is null) return null;
        if (syntax.Parent is null) return syntax.Range;

        return syntax.Parent switch
        {
            IfExpressionSyntax @if when ReferenceEquals(syntax, @if.Condition) =>
                new(@if.OpenParen.Range.Start, @if.CloseParen.Range.End),
            WhileExpressionSyntax @while when ReferenceEquals(syntax, @while.Condition) =>
                new(@while.OpenParen.Range.Start, @while.CloseParen.Range.End),
            _ => syntax.Range,
        };
    }

    private static bool IsCompoundExpression(BoundExpression expr) => expr switch
    {
        BoundBlockExpression or BoundWhileExpression or BoundIfExpression => true,
        _ => false,
    };
}
