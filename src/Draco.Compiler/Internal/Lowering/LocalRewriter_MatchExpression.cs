using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Internal.Lowering;

internal sealed partial class LocalRewriter
{
    public override BoundNode VisitMatchExpression(BoundMatchExpression node)
    {
        // match (matchedExpr) {
        //     pattern1 if (guard1) -> value1;
        //     pattern2 if (guard2) -> value2;
        //     ...
        // }
        //
        // =>
        //
        // {
        //     val tmp = matchedExpr;
        //     if (matches-pattern1(tmp) and guard1) value1
        //     else if (matches-pattern2(tmp) and guard2) value2
        //     ...
        // }

        // Evaluate the matched value as a local to not duplicate side-effects
        var matchedValue = (BoundExpression)node.MatchedValue.Accept(this);
        var tmp = this.StoreTemporary(matchedValue);

        var conditionValuePairs = new List<(BoundExpression Condition, BoundExpression Value)>();
        foreach (var arm in node.MatchArms)
        {
            var patternMatcher = this.ConstructPatternToMatch(arm.Pattern, tmp.Reference);
            var guard = arm.Guard ?? this.LiteralExpression(true);

            var condition = AndExpression(patternMatcher, guard);

            conditionValuePairs.Add((condition, arm.Value));
        }

        // NOTE: We do an r-fold to nest if-expressions to the right
        conditionValuePairs.Reverse();
        // TODO: The match-expr might be empty!
        var lastPair = conditionValuePairs[0];
        var ifChain = (BoundExpression)conditionValuePairs
            .Skip(1)
            .Aggregate(
                IfExpression(
                    lastPair.Condition,
                    lastPair.Value,
                    BoundTreeFactory.LiteralExpression(null, node.TypeRequired),
                    node.TypeRequired),
                (acc, arm) => IfExpression(arm.Condition, arm.Value, acc, node.TypeRequired))
            .Accept(this);

        return BlockExpression(
            locals: tmp.Symbol is null
                ? ImmutableArray<LocalSymbol>.Empty
                : ImmutableArray.Create(tmp.Symbol),
            statements: ImmutableArray.Create(tmp.Assignment),
            ifChain);
    }

    private BoundExpression ConstructPatternToMatch(BoundPattern pattern, BoundExpression matchedValue) => pattern switch
    {
        BoundDiscardPattern discard => this.ConstructPatternToMatch(discard, matchedValue),
        BoundLiteralPattern literal => this.ConstructPatternToMatch(literal, matchedValue),
        _ => throw new ArgumentOutOfRangeException(nameof(pattern)),
    };

    private BoundExpression ConstructPatternToMatch(BoundDiscardPattern pattern, BoundExpression matchedValue) =>
        // _ matches expr
        //
        // =>
        //
        // true
        this.LiteralExpression(true);

    private BoundExpression ConstructPatternToMatch(BoundLiteralPattern pattern, BoundExpression matchedValue) =>
        // N matches expr
        //
        // =>
        //
        // object.Equals(N, expr)
        CallExpression(
            receiver: null,
            method: this.WellKnownTypes.SystemObject_Equals,
            arguments: ImmutableArray.Create(
                this.LiteralExpression(pattern.Value),
                matchedValue));
}
