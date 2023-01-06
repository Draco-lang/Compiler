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
        private readonly ILattice<TElement> lattice;
        private readonly FlowDirection direction;

        public RecursionScheme(ILattice<TElement> lattice)
        {
            this.lattice = lattice;
            this.direction = lattice.Direction;
        }

        public bool Pass(Ast ast)
        {
            var identity = this.lattice.Identity;
            return this.Visit(ref identity, ast);
        }

        private bool Visit(ref TElement element, Ast ast) => ast switch
        {
            Ast.Decl.Variable var => this.Visit(ref element, var),
            Ast.Stmt.Decl decl => this.Visit(ref element, decl.Declaration),
            Ast.Stmt.Expr expr => this.Visit(ref element, expr.Expression),
            Ast.Expr.Block block => this.Visit(ref element, block),
            Ast.Expr.If @if => this.Visit(ref element, @if),
            Ast.Expr.While @while => this.Visit(ref element, @while),
            Ast.Expr.Literal => false,
            _ => throw new ArgumentOutOfRangeException(nameof(ast)),
        };

        private bool Visit(ref TElement element, Ast.Decl.Variable var)
        {
            if (var.Value is not null) this.Visit(ref element, var.Value);
            return this.lattice.Join(ref element, var);
        }

        private bool Visit(ref TElement element, Ast.Expr.Block block)
        {
            if (this.direction == FlowDirection.Forward)
            {
                foreach (var stmt in block.Statements) this.Visit(ref element, stmt);
                this.Visit(ref element, block.Value);
            }
            else
            {
                this.Visit(ref element, block.Value);
                for (var i = block.Statements.Length - 1; i >= 0; --i) this.Visit(ref element, block.Statements[i]);
            }
            return default;
        }

        private bool Visit(ref TElement element, Ast.Expr.If @if)
        {
            // Wrong! We need to recall old values to merge into!

            var changed = false;

            // Condition is always evaluated
            changed = this.Visit(ref element, @if.Condition) || changed;

            // Now we have two alternative paths
            var elementClone = this.lattice.Clone(element);
            changed = this.Visit(ref element, @if.Then) || changed;
            changed = this.Visit(ref elementClone, @if.Else) || changed;

            // TODO
            throw new NotImplementedException();
        }

        private bool Visit(ref TElement element, Ast.Expr.While @while)
        {
            // TODO
            return default;
        }
    }
}
