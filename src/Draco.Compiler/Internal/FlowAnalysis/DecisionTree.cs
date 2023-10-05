using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.FlowAnalysis.Domain;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.FlowAnalysis;

// TODO: CONSIDER GUARDS

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
    /// Represents a redundancy.
    /// </summary>
    /// <param name="CoveredBy">The action that covers the <paramref name="Redundant"/> one already.</param>
    /// <param name="Redundant">The action that is redundant, because <paramref name="CoveredBy"/> already
    /// matches.</param>
    public readonly record struct Redundance(TAction CoveredBy, TAction Redundant);

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
        /// The action that's associated with the node, in case it's a leaf.
        /// </summary>
        [MemberNotNullWhen(true, nameof(IsAction))]
        public abstract TAction? Action { get; }

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
        public override bool IsAction => this.Action is not null;
        public override TAction? Action => this.MutableAction;
        public override bool IsFail => this.PatternMatrix.Count == 0;
        public override BoundPattern? Counterexample => throw new NotImplementedException();
        public override BoundExpression? MatchedOn => throw new NotImplementedException();
        public override ImmutableArray<KeyValuePair<BoundPattern, Node>> Children =>
            this.builtChildren ??= this.MutableChildren
                .Select(n => new KeyValuePair<BoundPattern, Node>(n.Key, n.Value))
                .ToImmutableArray();
        private ImmutableArray<KeyValuePair<BoundPattern, Node>>? builtChildren;

        // Mutators
        public List<BoundExpression> Arguments { get; }
        public List<int> ArgumentOrder { get; }
        public List<List<BoundPattern>> PatternMatrix { get; }
        public List<TAction> Actions { get; }
        public TAction? MutableAction { get; set; }
        public List<KeyValuePair<BoundPattern, MutableNode>> MutableChildren { get; } = new();

        public MutableNode(
            Node? parent,
            List<BoundExpression> arguments,
            List<List<BoundPattern>> patternMatrix,
            List<TAction> actions)
        {
            this.Parent = parent;
            this.Arguments = arguments;
            this.ArgumentOrder = Enumerable
                .Range(0, this.Arguments.Count)
                .ToList();
            this.PatternMatrix = patternMatrix;
            this.Actions = actions;
        }

        public void SwapColumns(int i, int j)
        {
            Swap(this.Arguments, i, j);
            Swap(this.ArgumentOrder, i, j);
            foreach (var row in this.PatternMatrix) Swap(row, i, j);
        }

        private static void Swap<T>(List<T> list, int i, int j) => (list[i], list[j]) = (list[j], list[i]);
    }

    /// <summary>
    /// A comparer that compares patterns only in terms of specialization, not involving their
    /// arguments, if there are any.
    /// </summary>
    private sealed class SpecializationComparer : IEqualityComparer<BoundPattern>
    {
        public static SpecializationComparer Instance { get; } = new();

        private SpecializationComparer()
        {
        }

        public bool Equals(BoundPattern? x, BoundPattern? y) => (x, y) switch
        {
            _ => throw new ArgumentOutOfRangeException(paramName: null, message: "unhandled pair of patterns"),
        };

        public int GetHashCode([DisallowNull] BoundPattern obj) => obj switch
        {
            BoundDiscardPattern => typeof(BoundDiscardPattern).GetHashCode(),
            BoundLiteralPattern lit => lit.Value?.GetHashCode() ?? 0,
            _ => throw new ArgumentOutOfRangeException(nameof(obj)),
        };
    }

    /// <summary>
    /// Builds a decision tree from the given data.
    /// </summary>
    /// <param name="intrinsicSymbols">The intrinsic symbols of the compilation.</param>
    /// <param name="matchedValue">The matched value.</param>
    /// <param name="arms">The arms of the match.</param>
    /// <returns>The constructed decision tree.</returns>
    public static DecisionTree<TAction> Build(
        IntrinsicSymbols intrinsicSymbols,
        BoundExpression matchedValue,
        ImmutableArray<Arm> arms)
    {
        // Construct root
        var root = new MutableNode(
            parent: null,
            arguments: new List<BoundExpression> { matchedValue },
            patternMatrix: arms
                .Select(a => new List<BoundPattern> { a.Pattern })
                .ToList(),
            actions: arms
                .Select(a => a.Action)
                .ToList());
        // Wrap in the tree
        var tree = new DecisionTree<TAction>(intrinsicSymbols, root);
        // Build it
        tree.Build(root);
        return tree;
    }

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
    /// The root node of this tree.
    /// </summary>
    public Node Root => this.mutableRoot;
    private readonly MutableNode mutableRoot;

    /// <summary>
    /// All redundancies in the tree.
    /// </summary>
    public ImmutableArray<Redundance> Redundancies => this.redundancies.ToImmutable();
    private readonly ImmutableArray<Redundance>.Builder redundancies = ImmutableArray.CreateBuilder<Redundance>();

    /// <summary>
    /// True, if this tree is exhaustive.
    /// </summary>
    public bool IsExhaustive => GraphTraversal
        .DepthFirst(this.Root, n => n.Children.Select(c => c.Value))
        .All(n => !n.IsFail);

    /// <summary>
    /// An example of an uncovered pattern, if any.
    /// </summary>
    public BoundPattern? UncoveredExample => throw new NotImplementedException();

    private readonly IntrinsicSymbols intrinsicSymbols;

    private DecisionTree(IntrinsicSymbols intrinsicSymbols, MutableNode root)
    {
        this.intrinsicSymbols = intrinsicSymbols;
        this.mutableRoot = root;
    }

    private void Build(MutableNode node)
    {
        if (node.IsFail) return;

        if (node.PatternMatrix[0].All(MatchesEverything))
        {
            // This is a succeeding node, set the performed action
            node.MutableAction = node.Actions[0];
            // The remaining ones are redundant
            for (var i = 1; i < node.PatternMatrix.Count; ++i)
            {
                this.redundancies.Add(new(node.Actions[0], node.Actions[i]));
            }
            return;
        }

        // We need to make a decision, bring the column that has refutable entries to the beginning
        var firstColWithRefutable = FirstColumnWithRefutableEntry(node);
        if (firstColWithRefutable != 0) node.SwapColumns(0, firstColWithRefutable);

        // The first column now contains something that is refutable
        // Collect all pattern variants that we covered
        var coveredPatterns = node.PatternMatrix
            .Select(row => row[0])
            .Where(p => !MatchesEverything(p))
            .ToHashSet(SpecializationComparer.Instance);

        // Track if there are any uncovered values in this domain
        // TODO
        var uncoveredDomain = ValueDomain.CreateDomain(this.intrinsicSymbols, node.PatternMatrix[0][0].Type);

        // Specialize for each of these cases
        foreach (var pat in coveredPatterns)
        {
            // Specialize to the pattern
            this.Specialize(node, pat);
            // We covered the value, subtract
            uncoveredDomain.SubtractPattern(pat);
        }

        // If not complete, do defaulting
        if (!uncoveredDomain.IsEmpty) this.Default(node);

        // Recurse to children
        foreach (var (_, child) in node.MutableChildren) this.Build(child);
    }

    private MutableNode Specialize(MutableNode node, BoundPattern specializer) =>
        throw new NotImplementedException();

    private MutableNode Default(MutableNode node) =>
        throw new NotImplementedException();

    private static int FirstColumnWithRefutableEntry(MutableNode node)
    {
        for (var col = 0; col < node.PatternMatrix[0].Count; ++col)
        {
            if (node.PatternMatrix.Any(row => !MatchesEverything(row[col]))) return col;
        }

        throw new InvalidOperationException("should not happen");
    }

    private static bool MatchesEverything(BoundPattern pattern) => pattern switch
    {
        BoundDiscardPattern => true,
        _ => false,
    };
}
