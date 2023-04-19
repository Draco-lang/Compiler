using System.Collections.Immutable;
using System.Linq;
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
    public static BoundNode Inject(BoundNode node)
    {
        var rewriter = new SequencePointInjector(node);
        return node.Accept(rewriter);
    }

    private readonly BoundNode context;

    private SequencePointInjector(BoundNode context)
    {
        this.context = context;
    }

    public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
    {
        var injected = (BoundStatement)base.VisitLocalDeclaration(node);
        return SequencePointStatement(
            statement: injected,
            range: node.Syntax?.Range,
            // If the value is null, there is nothing to compile
            // So we enforce a NOP to be emitted
            // If value is not null, we at least have a store
            emitNop: node.Value is null);
    }

    public override BoundNode VisitLabelStatement(BoundLabelStatement node)
    {
        // NOTE: Labels don't need to be decorated with anything further
        // Since labels can be generated when the codegen is in a detached state,
        // we need to do a little tricklery to make the sequence-point valid
        // We do that like so:
        // {
        // label:
        //     <sequence point for label>
        //     nop
        // }
        return ExpressionStatement(BlockExpression(
            locals: ImmutableArray<LocalSymbol>.Empty,
            statements: ImmutableArray.Create<BoundStatement>(
                node,
                SequencePointStatement(
                    statement: null,
                    range: node.Syntax?.Range,
                    emitNop: true)),
            value: BoundUnitExpression.Default));
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
                emitNop: true);
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

        // Statements will wrap sequence points for themselves
        var statements = node.Statements
            .Select(s => s.Accept(this))
            .Cast<BoundStatement>()
            .ToImmutableArray();
        // If the expression is not compund, we wrap it
        var value = (BoundExpression)node.Value.Accept(this);
        if (!IsCompoundExpression(value))
        {
            value = SequencePointExpression(
                expression: value,
                range: node.Value.Syntax?.Range,
                emitNop: true);
        }

        var blockValue = new SynthetizedLocalSymbol(node.Type, false);

        return BlockExpression(
            locals: ImmutableArray.Create<LocalSymbol>(blockValue),
            statements: ImmutableArray.Create<BoundStatement>(
                SequencePointStatement(
                    statement: null,
                    range: openBrace.Range,
                    emitNop: true),
                LocalDeclaration(blockValue, BlockExpression(
                    locals: node.Locals,
                    statements: statements,
                    value: value)),
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
        // We only wrap the branches if they are not compound expressions
        // Compound expressions will wrap themselves nicely
        var then = (BoundExpression)node.Then.Accept(this);
        if (!IsCompoundExpression(then))
        {
            then = SequencePointExpression(
                expression: then,
                range: node.Then.Syntax?.Range,
                emitNop: true);
        }
        var @else = (BoundExpression)node.Else.Accept(this);
        if (!IsCompoundExpression(@else))
        {
            @else = SequencePointExpression(
                expression: @else,
                range: node.Else.Syntax?.Range,
                emitNop: true);
        }

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
        // We only wrap the body if it is not a compound expression
        // Compound expressions will wrap themselves nicely
        var then = (BoundExpression)node.Then.Accept(this);
        if (!IsCompoundExpression(then))
        {
            then = SequencePointExpression(
                expression: then,
                range: node.Then.Syntax?.Range,
                emitNop: true);
        }

        return WhileExpression(
            condition: condition,
            then: then,
            continueLabel: node.ContinueLabel,
            breakLabel: node.BreakLabel);
    }

    public override BoundNode VisitReturnExpression(BoundReturnExpression node)
    {
        var ancestorBlock = this.GetBlockFunctionBodyAncestor();
        if (ancestorBlock is null) return base.VisitReturnExpression(node);

        // We are in a block function, step onto the close brace before exiting
        //
        // func foo() {
        //     ...
        //     return value;
        //     ...
        // }
        //
        // =>
        //
        // func foo() {
        //     ...
        //     val storage = value; // Original return range
        //     return storage; // Sequence point pointing at close brace
        //     ...                |
        //                        |
        // } <--------------------+

        var storage = new SynthetizedLocalSymbol(node.Value.TypeRequired, false);
        var value = (BoundExpression)node.Value.Accept(this);

        return BlockExpression(
            locals: ImmutableArray.Create<LocalSymbol>(storage),
            statements: ImmutableArray.Create<BoundStatement>(
                LocalDeclaration(storage, value),
                SequencePointStatement(
                    statement: ExpressionStatement(ReturnExpression(LocalExpression(storage))),
                    range: ancestorBlock.CloseBrace.Range,
                    // We'll have a ret instruction
                    emitNop: false)),
            value: BoundUnitExpression.Default);
    }

    private BlockFunctionBodySyntax? GetBlockFunctionBodyAncestor()
    {
        var syntax = this.context.Syntax;
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
        BoundSequencePointExpression sp => IsCompoundExpression(sp.Expression),
        _ => false,
    };
}
