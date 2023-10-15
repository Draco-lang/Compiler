using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.FlowAnalysis;
using Draco.Compiler.Internal.Symbols;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Internal.Lowering;

internal sealed partial class LocalRewriter
{
    public override BoundNode VisitMatchExpression(BoundMatchExpression node)
    {
        // TODO: Elaborate on what we do here

        // Evaluate the matched value as a local to not duplicate side-effects
        var matchedValue = (BoundExpression)node.MatchedValue.Accept(this);
        var tmp = this.StoreTemporary(matchedValue);

        // We build up the relevant arms
        var arms = node.MatchArms
            .Select(a => DecisionTree.Arm(a.Pattern, a.Guard, a))
            .ToImmutableArray();
        // From that we build the decision tree
        var decisionTree = DecisionTree.Build(this.IntrinsicSymbols, tmp.Reference, arms);

        // Recursively lower each decision node
        var decisionNode = this.ConstructMatchNode(decisionTree.Root, node.TypeRequired);

        var result = BlockExpression(
            locals: tmp.Symbol is null
                ? ImmutableArray<LocalSymbol>.Empty
                : ImmutableArray.Create(tmp.Symbol),
            statements: ImmutableArray.Create(tmp.Assignment),
            decisionNode);
        return result.Accept(this);
    }

    private BoundExpression ConstructMatchNode(DecisionTree<BoundMatchArm>.INode node, TypeSymbol resultType)
    {
        // Hit a leaf
        if (node.IsAction) return (BoundExpression)node.Action.Value.Accept(this);

        // TODO
        // Failure
        if (node.IsFail) throw new NotImplementedException();

        // Fold backwards
        var seed = null as BoundExpression;
        for (var i = node.Children.Count - 1; i >= 0; --i)
        {
            var (cond, child) = node.Children[i];
            seed = Fold(seed, cond, child);
        }

        Debug.Assert(seed is not null);
        return seed;

        BoundExpression Fold(
            BoundExpression? prev,
            DecisionTree<BoundMatchArm>.Condition condition,
            DecisionTree<BoundMatchArm>.INode node)
        {
            var matchedValue = (BoundExpression)node.MatchedValue;
            var result = this.ConstructMatchNode(node, resultType);
            switch (condition.Pattern, condition.Guard)
            {
            case (null, null):
            {
                Debug.Assert(prev is null);
                return result;
            }
            case (BoundPattern pat, null):
            {
                var matchCondition = this.ConstructPatternToMatch(pat, matchedValue);
                return IfExpression(
                    condition: matchCondition,
                    then: result,
                    @else: prev ?? DefaultExpression(),
                    type: resultType);
            }
            case (null, BoundExpression cond):
            {
                return IfExpression(
                    condition: cond,
                    then: result,
                    @else: prev ?? DefaultExpression(),
                    type: resultType);
            }
            default:
                throw new InvalidOperationException();
            }
        }

        // TODO: As a default, we should THROW
        static BoundExpression DefaultExpression() => BoundUnitExpression.Default;
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
