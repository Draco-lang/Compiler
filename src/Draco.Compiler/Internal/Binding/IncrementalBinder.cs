using System;
using System.Collections.Generic;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Binding.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;

// NOTE: We don't follow the file-hierarchy here
// The reason is because this is a nested class of semantic model
namespace Draco.Compiler.Api.Semantics;

public sealed partial class SemanticModel
{
    /// <summary>
    /// Wraps another binder, filling out information about bound constructs.
    /// </summary>
    private sealed class IncrementalBinder(
        Binder underlyingBinder,
        SemanticModel semanticModel) : Binder(underlyingBinder.Compilation, underlyingBinder.Parent)
    {
        // NOTE: We only use the underlying binder for the lookup logic
        // For actual binding logic, we rely on the base class implementation
        // Otherwise, we escape memo context
        /// <summary>
        /// The binder being wrapped by this one.
        /// </summary>
        public Binder UnderlyingBinder { get; } = underlyingBinder;

        public override Symbol? ContainingSymbol => this.UnderlyingBinder.ContainingSymbol;

        public override SyntaxNode? DeclaringSyntax => this.UnderlyingBinder.DeclaringSyntax;

        public override IEnumerable<Symbol> DeclaredSymbols => this.UnderlyingBinder.DeclaredSymbols;

        protected override Binder GetBinder(SyntaxNode node)
        {
            var binder = base.GetBinder(node);
            return binder is IncrementalBinder
                ? binder
                : new IncrementalBinder(binder, semanticModel);
        }

        internal override void LookupLocal(LookupResult result, string name, ref LookupFlags flags, Predicate<Symbol> allowSymbol, SyntaxNode? currentReference) =>
            this.UnderlyingBinder.LookupLocal(result, name, ref flags, allowSymbol, currentReference);

        // API /////////////////////////////////////////////////////////////////

        public override BoundStatement BindFunction(SourceFunctionSymbol function, DiagnosticBag diagnostics) =>
            semanticModel.boundFunctions.GetOrAdd(
                key: function,
                valueFactory: _ => base.BindFunction(function, diagnostics));

        public override GlobalBinding BindGlobalField(SourceFieldSymbol global, DiagnosticBag diagnostics) =>
            semanticModel.boundGlobals.GetOrAdd(
                key: global,
                valueFactory: _ => base.BindGlobalField(global, diagnostics));

        // TODO: Do we want to override BindScript?

        // Memoizing overrides /////////////////////////////////////////////////

        protected override BindingTask<BoundStatement> BindStatement(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
            this.MemoizeBinding(syntax, constraints, () => base.BindStatement(syntax, constraints, diagnostics));

        internal override BindingTask<BoundExpression> BindExpression(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
            this.MemoizeBinding(syntax, constraints, () => base.BindExpression(syntax, constraints, diagnostics));

        protected override BindingTask<BoundLvalue> BindLvalue(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
            this.MemoizeBinding(syntax, constraints, () => base.BindLvalue(syntax, constraints, diagnostics));

        internal override Symbol BindLabel(LabelSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
            this.BindSymbol(syntax, () => base.BindLabel(syntax, constraints, diagnostics));

        internal override Symbol BindType(TypeSyntax syntax, DiagnosticBag diagnostics) =>
            this.BindSymbol(syntax, () => base.BindType(syntax, diagnostics));

        // TODO: Hack
        internal override void BindSyntaxToSymbol(SyntaxNode syntax, Symbol symbol) =>
            semanticModel.symbolMap[syntax] = symbol;

        internal override void BindTypeSyntaxToSymbol(SyntaxNode syntax, Internal.Symbols.TypeSymbol type)
        {
            semanticModel.symbolMap[syntax] = type;
            if (syntax.Parent is GenericExpressionSyntax) semanticModel.symbolMap[syntax.Parent] = type;
        }

        // Memo logic

        private async BindingTask<TBoundNode> MemoizeBinding<TBoundNode>(SyntaxNode syntax, ConstraintSolver constraints, Func<BindingTask<TBoundNode>> binder)
            where TBoundNode : BoundNode
        {
            if (!semanticModel.boundNodeMap.TryGetValue(syntax, out var node))
            {
                node = await binder();
                semanticModel.boundNodeMap.TryAdd(syntax, node);

                var symbol = ExtractSymbol(node);
                if (symbol is not null)
                {
                    foreach (var childSyntax in EnumerateSyntaxesWithSameSymbol(syntax))
                    {
                        semanticModel.symbolMap[childSyntax] = symbol;
                    }
                }
            }
            return (TBoundNode)node;
        }

        private Symbol BindSymbol(SyntaxNode node, Func<Symbol> binder) => semanticModel.symbolMap.GetOrAdd(
            key: node,
            valueFactory: _ => binder());

        private static IEnumerable<SyntaxNode> EnumerateSyntaxesWithSameSymbol(SyntaxNode node)
        {
            yield return node;
            if (node is CallExpressionSyntax call) yield return call.Function;
        }

        private static Symbol? ExtractSymbol(BoundNode node) => node switch
        {
            BoundLabelStatement l => l.Label,
            BoundParameterExpression p => p.Parameter,
            BoundLocalExpression l => l.Local,
            BoundReferenceErrorExpression e => e.Symbol,
            BoundLocalLvalue l => l.Local,
            BoundCallExpression c => c.Method,
            BoundFieldLvalue f => f.Field,
            BoundFieldExpression f => f.Field,
            BoundPropertyGetExpression p => (p.Getter as IPropertyAccessorSymbol)?.Property,
            BoundPropertySetExpression p => (p.Setter as IPropertyAccessorSymbol)?.Property,
            BoundIndexGetExpression i => (i.Getter as IPropertyAccessorSymbol)?.Property,
            BoundIndexSetExpression i => (i.Setter as IPropertyAccessorSymbol)?.Property,
            _ => null,
        };
    }
}
