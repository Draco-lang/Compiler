using System;
using System.Collections.Generic;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.UntypedTree;

// NOTE: We don't follow the file-hierarchy here
// The reason is because this is a nested class of semantic model
namespace Draco.Compiler.Api.Semantics;

public sealed partial class SemanticModel
{
    /// <summary>
    /// Wraps another binder, filling out information about bound constructs.
    /// </summary>
    private sealed class IncrementalBinder : Binder
    {
        // NOTE: We only use the underlying binder for the lookup logic
        // For actual binding logic, we rely on the base class implementation
        // Otherwise, we escape memo context
        /// <summary>
        /// The binder being wrapped by this one.
        /// </summary>
        public Binder UnderlyingBinder { get; }

        public override Symbol? ContainingSymbol => this.UnderlyingBinder.ContainingSymbol;

        public override SyntaxNode? DeclaringSyntax => this.UnderlyingBinder.DeclaringSyntax;

        public override IEnumerable<Symbol> DeclaredSymbols => this.UnderlyingBinder.DeclaredSymbols;

        private readonly SemanticModel semanticModel;

        public IncrementalBinder(Binder underlyingBinder, SemanticModel semanticModel)
            : base(underlyingBinder.Compilation, underlyingBinder.Parent)
        {
            this.UnderlyingBinder = underlyingBinder;
            this.semanticModel = semanticModel;
        }

        protected override Binder GetBinder(SyntaxNode node)
        {
            var binder = base.GetBinder(node);
            return binder is IncrementalBinder
                ? binder
                : new IncrementalBinder(binder, this.semanticModel);
        }

        internal override void LookupLocal(LookupResult result, string name, ref LookupFlags flags, Predicate<Symbol> allowSymbol, SyntaxNode? currentReference) =>
            this.UnderlyingBinder.LookupLocal(result, name, ref flags, allowSymbol, currentReference);

        internal override BoundStatement TypeStatement(UntypedStatement statement, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
            this.TypeNode(statement, () => base.TypeStatement(statement, constraints, diagnostics));

        internal override BoundExpression TypeExpression(UntypedExpression expression, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
            this.TypeNode(expression, () => base.TypeExpression(expression, constraints, diagnostics));

        internal override BoundLvalue TypeLvalue(UntypedLvalue lvalue, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
            this.TypeNode(lvalue, () => base.TypeLvalue(lvalue, constraints, diagnostics));

        internal override Symbol BindLabel(LabelSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
            this.LookupNode(syntax, () => base.BindLabel(syntax, constraints, diagnostics));

        internal override Symbol BindType(TypeSyntax syntax, DiagnosticBag diagnostics) =>
            this.LookupNode(syntax, () => base.BindType(syntax, diagnostics));

        internal override Symbol BindImportPath(ImportPathSyntax syntax, DiagnosticBag diagnostics) =>
            this.LookupNode(syntax, () => base.BindImportPath(syntax, diagnostics));

        internal override void BindModuleSyntaxToSymbol(SyntaxNode syntax, Internal.Symbols.ModuleSymbol module) =>
            this.semanticModel.symbolMap[syntax] = module;

        // TODO: There's nothing incremental in this,
        // but current usage doesn't require it either
        private TBoundNode TypeNode<TUntypedNode, TBoundNode>(TUntypedNode node, Func<TBoundNode> binder)
            where TUntypedNode : UntypedNode
            where TBoundNode : BoundNode
        {
            if (node.Syntax is null) return binder();
            // TODO: If the list is created, this node is bound already,
            // we should just search for the existing node somehow...
            // Problem is, we have multiple corresponding nodes
            // Maybe have an untyped -> typed map to resolve cases like this?
            if (!this.semanticModel.boundNodeMap.TryGetValue(node.Syntax, out var nodeList))
            {
                nodeList = new List<BoundNode>();
                this.semanticModel.boundNodeMap.Add(node.Syntax, nodeList);
            }
            var boundNode = binder();
            nodeList.Add(boundNode);
            var symbol = ExtractSymbol(boundNode);
            // TODO: Once the above is resolved, all we should have is an Add for safety
            if (symbol is not null) this.semanticModel.symbolMap[node.Syntax] = symbol;
            return boundNode;
        }

        // TODO: There's nothing incremental in this,
        // but current usage doesn't require it either
        private Symbol LookupNode(SyntaxNode node, Func<Symbol> binder)
        {
            if (!this.semanticModel.symbolMap.TryGetValue(node, out var symbol))
            {
                symbol = binder();
                this.semanticModel.symbolMap.Add(node, symbol);
            }
            return symbol;
        }

        private static Symbol? ExtractSymbol(BoundNode node) => node switch
        {
            BoundLocalDeclaration l => l.Local,
            BoundLabelStatement l => l.Label,
            BoundFunctionExpression f => f.Function,
            BoundParameterExpression p => p.Parameter,
            BoundLocalExpression l => l.Local,
            BoundGlobalExpression g => g.Global,
            BoundReferenceErrorExpression e => e.Symbol,
            BoundLocalLvalue l => l.Local,
            BoundGlobalLvalue g => g.Global,
            BoundMemberExpression m => m.Member,
            _ => null,
        };
    }
}
