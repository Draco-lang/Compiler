using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
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

        internal override BoundStatement TypeStatement(UntypedStatement statement, ConstraintBag constraints, DiagnosticBag diagnostics) =>
            this.TypeNode(statement, () => base.TypeStatement(statement, constraints, diagnostics));

        internal override BoundExpression TypeExpression(UntypedExpression expression, ConstraintBag constraints, DiagnosticBag diagnostics) =>
            this.TypeNode(expression, () => base.TypeExpression(expression, constraints, diagnostics));

        internal override BoundLvalue TypeLvalue(UntypedLvalue lvalue, ConstraintBag constraints, DiagnosticBag diagnostics) =>
            this.TypeNode(lvalue, () => base.TypeLvalue(lvalue, constraints, diagnostics));

        internal override Symbol BindLabel(LabelSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics) =>
            this.LookupNode(syntax, () => base.BindLabel(syntax, constraints, diagnostics));

        internal override Symbol BindType(TypeSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics) =>
            this.LookupNode(syntax, () => base.BindType(syntax, constraints, diagnostics));

        // TODO: There's nothing incremental in this,
        // but current usage doesn't require it either
        private TBoundNode TypeNode<TUntypedNode, TBoundNode>(TUntypedNode node, Func<TBoundNode> binder)
            where TUntypedNode : UntypedNode
            where TBoundNode : BoundNode
        {
            if (node.Syntax is null) return binder();
            if (!this.semanticModel.syntaxMap.TryGetValue(node.Syntax, out var nodeList))
            {
                nodeList = new List<BoundNode>();
                this.semanticModel.syntaxMap.Add(node.Syntax, nodeList);
            }
            var boundNode = binder();
            nodeList.Add(boundNode);
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
    }
}
