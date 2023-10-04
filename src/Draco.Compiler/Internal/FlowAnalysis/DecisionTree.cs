using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// Represents a decision tree computed for a match expression.
/// It helps guiding the rewriter to generate code as well as reporting things like redundant branches or
/// missing cases.
/// </summary>
/// <typeparam name="TAction">The action type being performed, when a branch is matched.</typeparam>
internal sealed class DecisionTree<TAction>
{
    /// <summary>
    /// A single arm in the match construct.
    /// </summary>
    /// <param name="Pattern">The matched pattern.</param>
    /// <param name="Action">The taken action if <paramref name="Pattern"/> matches.</param>
    public readonly record struct Arm(BoundPattern Pattern, TAction Action);

    /// <summary>
    /// Represents a redundant arm.
    /// </summary>
    /// <param name="CoveredBy">The arm that covers the <paramref name="Redundant"/> one already.</param>
    /// <param name="Redundant">The arm that is redundant, because <paramref name="CoveredBy"/> already
    /// matches.</param>
    public readonly record struct Redundance(Arm CoveredBy, Arm Redundant);

    /// <summary>
    /// A single node in the decision tree.
    /// </summary>
    public abstract class Node
    {
        /// <summary>
        /// The parent node of this one.
        /// </summary>
        public abstract Node? Parent { get; }

        /// <summary>
        /// True, if this is an action node, meaning that there is a match.
        /// </summary>
        public abstract bool IsAction { get; }

        /// <summary>
        /// The arm that's associated with the node.
        /// </summary>
        [MemberNotNullWhen(true, nameof(IsAction))]
        public abstract Arm? Arm { get; }

        /// <summary>
        /// True, if this is a failure node.
        /// </summary>
        public abstract bool IsFail { get; }

        /// <summary>
        /// The counterexample of this node, if it's a failure node and a counterexample could be produced.
        /// The counterexample in this case means a pattern that was not covered.
        /// </summary>
        public abstract BoundPattern? Counterexample { get; }

        /// <summary>
        /// The expression being matched on, if any.
        /// </summary>
        public abstract BoundExpression? MatchedOn { get; }

        /// <summary>
        /// The child nodes of this one, associated to the pattern to match to take the route to the child.
        /// </summary>
        public abstract ImmutableArray<KeyValuePair<BoundPattern, Node>> Children { get; }
    }

    private sealed class MutableNode : Node
    {
        // Observers
        public override Node? Parent { get; }
        public override bool IsAction => this.Arm is not null;
        public override Arm? Arm => this.MatchingArm;
        public override bool IsFail => this.PatternMatrix.Count == 0;
        public override BoundPattern? Counterexample => throw new NotImplementedException();
        public override BoundExpression? MatchedOn => throw new NotImplementedException();
        public override ImmutableArray<KeyValuePair<BoundPattern, Node>> Children =>
            this.builtChildren ??= this.MutableChildren.ToImmutableArray();
        private ImmutableArray<KeyValuePair<BoundPattern, Node>>? builtChildren;

        // Mutators
        public List<BoundExpression> Arguments { get; }
        public List<int> ArgumentOrder { get; }
        public List<List<BoundPattern>> PatternMatrix { get; }
        public List<Arm> Arms { get; }
        public Arm? MatchingArm { get; set; }
        public List<KeyValuePair<BoundPattern, Node>> MutableChildren { get; }
    }

    /// <summary>
    /// Builds a decision tree from the given data.
    /// </summary>
    /// <param name="matchedValue">The matched value.</param>
    /// <param name="arms">The arms of the match.</param>
    /// <returns>The constructed decision tree.</returns>
    public static DecisionTree<TAction> Build(BoundExpression matchedValue, ImmutableArray<Arm> arms) =>
        throw new NotImplementedException();

    /// <summary>
    /// Stringifies the given <paramref name="pattern"/> to a user-readable format.
    /// </summary>
    /// <param name="pattern">The pattern to stringify.</param>
    /// <returns>The user-readable form of <paramref name="pattern"/>.</returns>
    public static string ToDisplayString(BoundPattern pattern) => pattern switch
    {
        _ => throw new ArgumentOutOfRangeException(nameof(pattern)),
    };

    /// <summary>
    /// The matched value.
    /// </summary>
    public BoundExpression MatchedValue { get; }

    /// <summary>
    /// The arms of the root construct.
    /// </summary>
    public ImmutableArray<Arm> Arms { get; }

    /// <summary>
    /// The root node of this tree.
    /// </summary>
    public Node Root { get; }

    /// <summary>
    /// All redundancies in the tree.
    /// </summary>
    public ImmutableArray<Redundance> Redundancies { get; }

    /// <summary>
    /// True, if this tree is exhaustive.
    /// </summary>
    public bool IsExhaustive { get; }

    /// <summary>
    /// An example of an uncovered pattern, if any.
    /// </summary>
    public BoundPattern? UncoveredExample { get; }

    private DecisionTree()
    {
        // TODO
    }
}
