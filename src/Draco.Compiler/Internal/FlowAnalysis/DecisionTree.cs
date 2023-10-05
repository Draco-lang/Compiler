using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.FlowAnalysis.Domain;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// Utilities for building a decision tree.
/// </summary>
internal static class DecisionTree
{
    /// <summary>
    /// Builds a decision tree from the given data.
    /// </summary>
    /// <typeparam name="TAction">The action type.</typeparam>
    /// <param name="intrinsicSymbols">The intrinsic symbols of the compilation.</param>
    /// <param name="matchedValue">The matched value.</param>
    /// <param name="arms">The arms of the match.</param>
    /// <returns>The constructed decision tree.</returns>
    public static DecisionTree<TAction> Build<TAction>(
        IntrinsicSymbols intrinsicSymbols,
        BoundExpression matchedValue,
        ImmutableArray<DecisionTree<TAction>.Arm> arms)
        where TAction : class =>
        DecisionTree<TAction>.Build(intrinsicSymbols, matchedValue, arms);

    /// <summary>
    /// Utility for constructing an arm for a decision tree.
    /// </summary>
    /// <typeparam name="TAction">The action type.</typeparam>
    /// <param name="pattern">The arm pattern.</param>
    /// <param name="guard">The guard expression, if any.</param>
    /// <param name="action">The associated action.</param>
    /// <returns>The constructed decision tree arm.</returns>
    public static DecisionTree<TAction>.Arm Arm<TAction>(BoundPattern pattern, BoundExpression? guard, TAction action)
        where TAction : class => new(pattern, guard, action);

    /// <summary>
    /// Stringifies the given <paramref name="pattern"/> to a user-readable format.
    /// </summary>
    /// <param name="pattern">The pattern to stringify.</param>
    /// <returns>The user-readable form of <paramref name="pattern"/>.</returns>
    public static string ToDisplayString(BoundPattern pattern) => pattern switch
    {
        BoundDiscardPattern => "_",
        BoundLiteralPattern lit => lit.Value?.ToString() ?? "null",
        _ => throw new ArgumentOutOfRangeException(nameof(pattern)),
    };
}

/// <summary>
/// Represents a decision tree computed for a match expression.
/// It helps guiding the rewriter to generate code as well as reporting things like redundant branches or
/// missing cases.
/// </summary>
/// <typeparam name="TAction">The action type being performed, when a branch is matched.</typeparam>
internal sealed class DecisionTree<TAction>
    where TAction : class
{
    /// <summary>
    /// Builds a decision tree from the given data.
    /// </summary>
    /// <param name="intrinsicSymbols">The intrinsic symbols of the compilation.</param>
    /// <param name="matchedValue">The matched value.</param>
    /// <param name="arms">The arms of the match.</param>
    /// <returns>The constructed decision tree.</returns>
    public static DecisionTree<TAction> Build(IntrinsicSymbols intrinsicSymbols, BoundExpression matchedValue, ImmutableArray<Arm> arms)
    {
        // Construct root
        var root = new Node(
            parent: null,
            arguments: new List<BoundExpression> { matchedValue },
            patternMatrix: arms
                .Select(a => new List<BoundPattern> { a.Pattern })
                .ToList(),
            actionArms: arms.ToList());
        // Wrap in the tree
        var tree = new DecisionTree<TAction>(intrinsicSymbols, root);
        // Build it
        tree.Build(root);
        // Done
        return tree;
    }

    /// <summary>
    /// A single arm in the match construct.
    /// </summary>
    /// <param name="Pattern">The matched pattern.</param>
    /// <param name="Guard">The guard of the arm, if any.</param>
    /// <param name="Action">The taken action if <paramref name="Pattern"/> matches.</param>
    public readonly record struct Arm(BoundPattern Pattern, BoundExpression? Guard, TAction Action);

    /// <summary>
    /// Represents a redundancy.
    /// </summary>
    /// <param name="CoveredBy">The action that covers the <paramref name="Uselesss"/> one already.</param>
    /// <param name="Uselesss">The action that is redundant or useless, because <paramref name="CoveredBy"/> already
    /// matches.</param>
    public readonly record struct Redundance(TAction CoveredBy, TAction Uselesss);

    /// <summary>
    /// A single node in the decision tree.
    /// </summary>
    public interface INode
    {
        /// <summary>
        /// The parent node of this one, if any.
        /// </summary>
        public INode? Parent { get; }

        /// <summary>
        /// True, if this is an action node, meaning that there is a match.
        /// </summary>
        public bool IsAction { get; }

        /// <summary>
        /// The action arm that's associated with the node, in case it's a leaf.
        /// </summary>
        [MemberNotNullWhen(true, nameof(IsAction))]
        public Arm? ActionArm { get; }

        /// <summary>
        /// The action that's associated with the node, in case it's a leaf.
        /// </summary>
        [MemberNotNullWhen(true, nameof(IsAction))]
        public TAction? Action { get; }

        /// <summary>
        /// True, if this is a failure node.
        /// </summary>
        public bool IsFail { get; }

        /// <summary>
        /// An example pattern that this node did not cover.
        /// </summary>
        public BoundPattern? NotCovered { get; }

        /// <summary>
        /// The expression being matched on.
        /// </summary>
        public BoundExpression MatchedValue { get; }

        /// <summary>
        /// The child nodes of this one, associated to the pattern to match to take the route to the child.
        /// </summary>
        public IReadOnlyList<KeyValuePair<BoundPattern, INode>> Children { get; }
    }

    // A mutable node implementation
    private sealed class Node : INode
    {
        public INode? Parent { get; }
        public bool IsAction => this.ActionArm is not null;
        public Arm? ActionArm { get; set; }
        public TAction? Action => this.ActionArm?.Action;
        public bool IsFail => this.PatternMatrix.Count == 0;
        // TODO
        public BoundPattern? NotCovered => throw new NotImplementedException();
        public BoundExpression MatchedValue => this.Arguments[0];
        public List<KeyValuePair<BoundPattern, INode>> Children { get; } = new();
        IReadOnlyList<KeyValuePair<BoundPattern, INode>> INode.Children => this.Children;

        public List<BoundExpression> Arguments { get; }
        public List<int> ArgumentOrder { get; }
        public List<List<BoundPattern>> PatternMatrix { get; }
        public List<Arm> ActionArms { get; }

        public int FirstColumnWithRefutableEntry
        {
            get
            {
                for (var col = 0; col < this.PatternMatrix[0].Count; ++col)
                {
                    if (this.PatternMatrix.Any(row => !MatchesEverything(row[col]))) return col;
                }

                throw new InvalidOperationException("the matrix only contains irrefutable entries");
            }
        }

        public Node(
            INode? parent,
            List<BoundExpression> arguments,
            List<List<BoundPattern>> patternMatrix,
            List<Arm> actionArms)
        {
            this.Parent = parent;
            this.Arguments = arguments;
            this.ArgumentOrder = Enumerable
                .Range(0, this.Arguments.Count)
                .ToList();
            this.PatternMatrix = patternMatrix;
            this.ActionArms = actionArms;
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
        /// <summary>
        /// A singleton instance that can be used.
        /// </summary>
        public static SpecializationComparer Instance { get; } = new();

        private SpecializationComparer()
        {
        }

        public bool Equals(BoundPattern? x, BoundPattern? y) => (x, y) switch
        {
            (BoundDiscardPattern, BoundDiscardPattern) => true,
            (BoundLiteralPattern lit1, BoundLiteralPattern lit2) => Equals(lit1.Value, lit2.Value),
            _ => false,
        };

        public int GetHashCode([DisallowNull] BoundPattern obj) => obj switch
        {
            BoundDiscardPattern => 0,
            BoundLiteralPattern lit => lit.Value?.GetHashCode() ?? 0,
            _ => throw new ArgumentOutOfRangeException(nameof(obj)),
        };
    }

    /// <summary>
    /// Checks, if the given pattern matches any value, making it irrefutable.
    /// </summary>
    /// <param name="pattern">The pattern to check.</param>
    /// <returns>True, if <paramref name="pattern"/> matches any possible value.</returns>
    private static bool MatchesEverything(BoundPattern pattern) => pattern switch
    {
        BoundDiscardPattern => true,
        _ => false,
    };

    /// <summary>
    /// Attempts to explode a pattern with its arguments for specialization.
    /// </summary>
    /// <param name="specializer">The specializing pattern.</param>
    /// <param name="toExplode">The pattern to explode.</param>
    /// <returns>The exploded arguments of <paramref name="toExplode"/> if it can be specialized by
    /// <paramref name="specializer"/>, null otherwise.</returns>
    private static ImmutableArray<BoundPattern>? TryExplode(BoundPattern specializer, BoundPattern toExplode) => (specializer, toExplode) switch
    {
        (BoundDiscardPattern, _) => throw new ArgumentOutOfRangeException(nameof(specializer)),
        (BoundLiteralPattern, BoundDiscardPattern) => ImmutableArray<BoundPattern>.Empty,
        (BoundLiteralPattern lit1, BoundLiteralPattern lit2) when Equals(lit1.Value, lit2.Value) => ImmutableArray<BoundPattern>.Empty,
        _ => null,
    };

    /// <summary>
    /// The root node of this tree.
    /// </summary>
    public INode Root => this.root;
    private readonly Node root;

    /// <summary>
    /// All redundancies in the tree.
    /// </summary>
    public IReadOnlySet<Redundance> Redundancies => this.redundancies;
    private HashSet<Redundance> redundancies = new();

    /// <summary>
    /// True, if this tree is exhaustive.
    /// </summary>
    public bool IsExhaustive => GraphTraversal
        .DepthFirst(this.Root, n => n.Children.Select(c => c.Value))
        .All(n => !n.IsFail);

    /// <summary>
    /// An example of an unhandled pattern, if any.
    /// </summary>
    public BoundPattern? UnhandledExample => throw new NotImplementedException();

    private readonly IntrinsicSymbols intrinsicSymbols;

    private DecisionTree(IntrinsicSymbols intrinsicSymbols, Node root)
    {
        this.intrinsicSymbols = intrinsicSymbols;
        this.root = root;
    }

    /// <summary>
    /// Builds the subtree.
    /// </summary>
    /// <param name="node">The current root of the tree.</param>
    private void Build(Node node)
    {
        // TODO: Handle guards!
        // They complicate analysis quite a bit

        if (node.IsFail) return;

        if (node.PatternMatrix[0].All(MatchesEverything))
        {
            // This is a succeeding node, set the performed action
            node.ActionArm = node.ActionArms[0];
            // The remaining ones are redundant
            for (var i = 1; i < node.PatternMatrix.Count; ++i)
            {
                this.redundancies.Add(new(node.ActionArms[0].Action, node.ActionArms[i].Action));
            }
            return;
        }

        // We need to make a decision, bring the column that has refutable entries to the beginning
        var firstColWithRefutable = node.FirstColumnWithRefutableEntry;
        if (firstColWithRefutable != 0) node.SwapColumns(0, firstColWithRefutable);

        // The first column now contains something that is refutable
        // Collect all pattern variants that we covered
        var coveredPatterns = node.PatternMatrix
            .Select(row => row[0])
            .Where(p => !MatchesEverything(p))
            .ToHashSet(SpecializationComparer.Instance);

        // Track if there are any uncovered values in this domain
        var uncoveredDomain = ValueDomain.CreateDomain(this.intrinsicSymbols, node.PatternMatrix[0][0].Type);

        // Specialize for each of these cases
        foreach (var pat in coveredPatterns)
        {
            // Specialize to the pattern
            var child = this.Specialize(node, pat);
            // We covered the value, subtract
            uncoveredDomain.SubtractPattern(pat);
            // Add as child
            node.Children.Add(new(pat, child));
        }

        // If not complete, do defaulting
        if (!uncoveredDomain.IsEmpty)
        {
            var @default = this.Default(node);
            // Add as child
            node.Children.Add(new(BoundDiscardPattern.Default, @default));
        }

        // Recurse to children
        foreach (var (_, child) in node.Children) this.Build((Node)child);
    }

    /// <summary>
    /// Specializes the given node.
    /// </summary>
    /// <param name="node">The node to specialize.</param>
    /// <param name="specializer">The specializer pattern.</param>
    /// <returns>The <paramref name="node"/> specialized by <paramref name="specializer"/>.</returns>
    private Node Specialize(Node node, BoundPattern specializer)
    {
        var remainingRows = new List<List<BoundPattern>>();
        var remainingActions = new List<Arm>();
        var hadEmptyRow = false;
        foreach (var (row, action) in node.PatternMatrix.Zip(node.ActionArms))
        {
            var exploded = TryExplode(specializer, row[0]);
            if (exploded is null) continue;

            var newRow = exploded.Concat(row.Skip(1)).ToList();
            if (newRow.Count == 0)
            {
                hadEmptyRow = true;
                newRow.Add(BoundDiscardPattern.Default);
            }

            // NOTE: Row is already cloned
            remainingRows.Add(newRow);
            remainingActions.Add(action);
        }

        var newArguments = Enumerable
            .Range(0, remainingRows[0].Count - node.PatternMatrix[0].Count + 1)
            .Select(i => hadEmptyRow
                ? node.Arguments[0]
                // TODO: Construct accessor for i-th element
                : throw new NotImplementedException("we don't handle accessor construction yet"))
            .Concat(node.Arguments.Skip(1))
            .ToList();

        return new(
            parent: node,
            arguments: newArguments,
            patternMatrix: remainingRows,
            actionArms: remainingActions);
    }

    /// <summary>
    /// Constructs the default node, which keeps only the irrefutable elements.
    /// </summary>
    /// <param name="node">The node to construct the default of.</param>
    /// <returns>The defaulted <paramref name="node"/>.</returns>
    private Node Default(Node node)
    {
        // Keep only irrefutable rows
        var remainingRows = new List<List<BoundPattern>>();
        var remainingActions = new List<Arm>();
        foreach (var (row, action) in node.PatternMatrix.Zip(node.ActionArms))
        {
            if (!MatchesEverything(row[0])) continue;

            // NOTE: We need to clone in case it gets mutated
            remainingRows.Add(row.ToList());
            remainingActions.Add(action);
        }
        return new(
            parent: node,
            arguments: node.Arguments,
            patternMatrix: remainingRows,
            actionArms: remainingActions);
    }
}
