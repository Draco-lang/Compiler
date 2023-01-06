using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Performs data-flow analysis based on a lattice.
/// </summary>
internal static class DataFlowAnalysis
{
    public static void Analyze<TElement>(ILattice<TElement> lattice, Ast ast)
    {
        var scheme = new RecursionScheme<TElement>(lattice);
        // Loop until no change
        while (scheme.Pass(ast)) ;
        // TODO: return something?
    }

    private sealed class RecursionScheme<TElement>
    {
        private sealed class FlowInfo
        {
            // NOTE: Field because we pass by ref
            public TElement Element;

            public FlowInfo(TElement element)
            {
                this.Element = element;
            }
        }

        private readonly ILattice<TElement> lattice;
        private readonly FlowDirection direction;
        private readonly Dictionary<object, FlowInfo> blockElements = new(ReferenceEqualityComparer.Instance);
        private readonly FlowInfo initialInfo;

        private bool hasChanged;

        public RecursionScheme(ILattice<TElement> lattice)
        {
            this.lattice = lattice;
            this.direction = lattice.Direction;
            this.initialInfo = new(lattice.Identity);
        }

        public bool Pass(Ast ast)
        {
            this.hasChanged = false;
            this.Visit(this.initialInfo, ast);
            return this.hasChanged;
        }

        private void Meet(FlowInfo incoming, Ast ast)
        {
            if (!this.blockElements.TryGetValue(ast, out var info))
            {
                info = new(this.lattice.Identity);
                this.blockElements.Add(ast, info);
            }
            this.hasChanged = this.lattice.Meet(ref info.Element, incoming.Element) || this.hasChanged;
        }

        private Unit Visit(FlowInfo info, Ast ast) => ast switch
        {
            Ast.Decl.Variable var => this.Visit(info, var),
            Ast.Stmt.Decl decl => this.Visit(info, decl.Declaration),
            Ast.Stmt.Expr expr => this.Visit(info, expr.Expression),
            Ast.Expr.Block block => this.Visit(info, block),
            Ast.Expr.If @if => this.Visit(info, @if),
            Ast.Expr.While @while => this.Visit(info, @while),
            Ast.Expr.Return @return => this.Visit(info, @return),
            Ast.Expr.Literal => default,
            Ast.Expr.Unit => default,
            _ => throw new ArgumentOutOfRangeException(nameof(ast)),
        };

        private Unit Visit(FlowInfo info, Ast.Decl.Variable var)
        {
            if (var.Value is not null) this.Visit(info, var.Value);
            this.hasChanged = this.lattice.Join(ref info.Element, var) || this.hasChanged;
            return default;
        }

        private Unit Visit(FlowInfo info, Ast.Expr.Block block)
        {
            if (this.direction == FlowDirection.Forward)
            {
                foreach (var stmt in block.Statements) this.Visit(info, stmt);
                this.Visit(info, block.Value);
            }
            else
            {
                this.Visit(info, block.Value);
                for (var i = block.Statements.Length - 1; i >= 0; --i) this.Visit(info, block.Statements[i]);
            }
            return default;
        }

        private Unit Visit(FlowInfo info, Ast.Expr.If @if)
        {
            if (this.direction == FlowDirection.Forward)
            {
                // Condition is always execuded
                this.Visit(info, @if.Condition);

                // Clone the info, as there are two alternative paths
                var altInfo = new FlowInfo(this.lattice.Clone(info.Element));

                // Take the alternative paths
                this.Visit(info, @if.Then);
                this.Visit(altInfo, @if.Else);

                // Merge
                this.hasChanged = this.lattice.Meet(ref info.Element, altInfo.Element) || this.hasChanged;
            }
            else
            {
                // Clone the info, as there are two alternative paths
                var altInfo = new FlowInfo(this.lattice.Clone(info.Element));

                // Take the alternative paths
                this.Visit(info, @if.Then);
                this.Visit(altInfo, @if.Else);

                // Merge
                this.hasChanged = this.lattice.Meet(ref info.Element, altInfo.Element) || this.hasChanged;

                // Condition is always execuded
                this.Visit(info, @if.Condition);
            }
            return default;
        }

        private Unit Visit(FlowInfo info, Ast.Expr.While @while)
        {
            // TODO
            throw new NotImplementedException();
            return default;
        }

        private Unit Visit(FlowInfo info, Ast.Expr.Return @return)
        {
            this.Visit(info, @return.Expression);
            this.hasChanged = this.lattice.Join(ref info.Element, @return) || this.hasChanged;
            return default;
        }
    }
}
