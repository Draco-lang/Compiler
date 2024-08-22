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
    /// <summary>
    /// Represents a value that was temporarily stored.
    /// </summary>
    /// <param name="Symbol">The synthetized local symbol.</param>
    /// <param name="Reference">The expression referencing the stored temporary.</param>
    /// <param name="Assignment">The assignment that stores the temporary.</param>
    private readonly record struct TemporaryStorage(
        LocalSymbol Symbol,
        BoundExpression Reference,
        BoundStatement Assignment);

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
            locals: [],
            statements:
            [
                node,
                SequencePointStatement(
                    statement: null,
                    range: node.Syntax?.Range,
                    emitNop: true),
            ],
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
            locals: [blockValue],
            statements:
            [
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
,
            ],
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

    public override BoundNode VisitForExpression(BoundForExpression node)
    {
        // For loops are a bit more complex, transformation can be found below
        //
        // for (i in SomeSequence) body;
        //
        // =>
        //
        // val storedSequence = SomeSequence; // first sequence point, pointing at 'SomeSequence'
        // for (newIterator in storedSequence) {
        //                     ^^^^^^^^^^^^^^ second sequence point for 'in' keyword
        //     val i = newIterator; // third sequence point, pointing at 'i'
        //     body;
        // }

        var syntax = node.Syntax as ForExpressionSyntax;

        // Wrap the sequence and store it
        var sequence = (BoundExpression)node.Sequence.Accept(this);
        var sequenceStorage = StoreTemporary(sequence);
        var sequenceAssignment = SequencePointStatement(
            statement: sequenceStorage.Assignment,
            range: node.Sequence.Syntax?.Range,
            emitNop: true);

        // Wrap the sequence reference for 'in'
        var sequenceIn = SequencePointExpression(
            expression: sequenceStorage.Reference,
            range: syntax?.InKeyword.Range,
            emitNop: true);

        // Create a new iterator variable
        var newIterator = new SynthetizedLocalSymbol(node.Iterator.Type, false);

        // Create the assignment, pointing at the iterator syntax
        var iteratorAssignment = SequencePointStatement(
            statement: LocalDeclaration(node.Iterator, LocalExpression(newIterator)),
            range: syntax?.Iterator.Range,
            emitNop: true);

        // Wrap the body
        var then = (BoundExpression)node.Then.Accept(this);

        // Reconstruction
        return BlockExpression(
            locals: [sequenceStorage.Symbol],
            statements:
            [
                sequenceAssignment,
                ExpressionStatement(ForExpression(
                    iterator: newIterator,
                    sequence: sequenceIn,
                    then: BlockExpression(
                        locals: [node.Iterator],
                        statements: [
                            iteratorAssignment,
                            ExpressionStatement(then),
                        ],
                        value: BoundUnitExpression.Default),
                    continueLabel: node.ContinueLabel,
                    breakLabel: node.BreakLabel,
                    getEnumeratorMethod: node.GetEnumeratorMethod,
                    moveNextMethod: node.MoveNextMethod,
                    currentProperty: node.CurrentProperty)),
            ],
            value: BoundUnitExpression.Default);
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
            locals: [storage],
            statements:
            [
                LocalDeclaration(storage, value),
                SequencePointStatement(
                    statement: ExpressionStatement(ReturnExpression(LocalExpression(storage))),
                    range: ancestorBlock.CloseBrace.Range,
                    // We'll have a ret instruction
                    emitNop: false),
            ],
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

    // Utility to store an expression to a temporary variable
    // NOTE: Almost copypaste from local rewriter, but we always store here
    // this is because here the goal is not eliminating double evaluation,
    // but to provide a sequence point for a given hidden expression
    // We also DO NOT call lowering anywhere
    // If the expression passed in is assumed to be injected, caller should cater for that
    private static TemporaryStorage StoreTemporary(BoundExpression expr)
    {
        var symbol = new SynthetizedLocalSymbol(expr.TypeRequired, false);
        var symbolRef = LocalExpression(symbol);
        var assignment = LocalDeclaration(local: symbol, value: expr);
        return new(symbol, symbolRef, assignment);
    }

    private static bool IsCompoundExpression(BoundExpression expr) => expr switch
    {
        BoundBlockExpression
     or BoundWhileExpression
     or BoundIfExpression
     or BoundForExpression => true,
        BoundSequencePointExpression sp => IsCompoundExpression(sp.Expression),
        _ => false,
    };
}
