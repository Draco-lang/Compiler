using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Utilities for invoking data-flow analysis.
/// </summary>
internal static class DataFlowAnalysis
{
    // TODO: Doc
    public static void Analyze<TElement>(ILattice<TElement> lattice, Ast ast) =>
        DataFlowAnalysis<TElement>.Analyze(lattice, ast);
}

/// <summary>
/// Performs data-flow analysis using a lattice.
/// </summary>
/// <typeparam name="TElement">The lattice element type.</typeparam>
internal sealed class DataFlowAnalysis<TElement>
{
    // TODO: Doc
    public static void Analyze(ILattice<TElement> lattice, Ast ast)
    {
        var analyzer = new DataFlowAnalysis<TElement>(lattice);
        while (analyzer.Pass(ast)) ;
        // TODO: What to return?
    }

    private sealed class FlowInfo
    {
        public bool Changed { get; set; }
        // NOTE: field so we can pass by ref
        public TElement Element;

        private readonly ILattice<TElement> lattice;

        public FlowInfo(ILattice<TElement> lattice, TElement element)
        {
            this.lattice = lattice;
            this.Element = element;
        }

        public void Meet(FlowInfo other)
        {
            this.Changed = this.lattice.Meet(ref this.Element, other.Element) || this.Changed;
        }

        public void Transfer(Ast node)
        {
            // TODO: Ugly hack
            this.Changed = this.lattice.Transfer(ref this.Element, node as dynamic) || this.Changed;
        }

        public FlowInfo Clone() =>
            new(lattice: this.lattice, element: this.lattice.Clone(this.Element));
    }

    // The lattice driving the analysis
    private readonly ILattice<TElement> lattice;
    // The initial info
    private readonly FlowInfo initialInfo;
    // Back-referenced states that need to be recalled
    private readonly Dictionary<ISymbol.ILabel, FlowInfo> backReferences = new();

    private DataFlowAnalysis(ILattice<TElement> lattice)
    {
        this.lattice = lattice;
        this.initialInfo = new(this.lattice, lattice.Identity);

        if (lattice.Direction != FlowDirection.Forward) throw new NotImplementedException();
    }

    private FlowInfo GetInfo(ISymbol.ILabel label)
    {
        if (!this.backReferences.TryGetValue(label, out var info))
        {
            info = new(this.lattice, this.lattice.Identity);
            this.backReferences.Add(label, info);
        }
        return info;
    }

    private bool Pass(Ast node)
    {
        // Reset changed info
        this.initialInfo.Changed = false;
        foreach (var info in this.backReferences.Values) info.Changed = false;

        // Perform pass
        this.Visit(this.initialInfo, node);

        // Aggregate status
        return this.initialInfo.Changed
            || this.backReferences.Values.Any(i => i.Changed);
    }

    private Unit Visit(FlowInfo prev, Ast node) => node switch
    {
        Ast.Decl.Variable n => this.Visit(prev, n),
        Ast.Decl.Label n => this.Visit(prev, n),
        Ast.Stmt.Decl n => this.Visit(prev, n.Declaration),
        Ast.Stmt.Expr n => this.Visit(prev, n.Expression),
        Ast.Expr.Block n => this.Visit(prev, n),
        Ast.Expr.Goto n => this.Visit(prev, n),
        Ast.Expr.Return n => this.Visit(prev, n),
        Ast.Expr.If n => this.Visit(prev, n),
        Ast.Expr.While n => this.Visit(prev, n),
        // NOTE: Not relevant for flow analysis
        Ast.Expr.Literal or Ast.Expr.Unit => default,
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };

    private Unit Visit(FlowInfo prev, Ast.Decl.Label node)
    {
        // We need to recall the place and meet into the current state
        var labelInfo = this.GetInfo(node.LabelSymbol);
        prev.Meet(labelInfo);

        return default;
    }

    private Unit Visit(FlowInfo prev, Ast.Expr.Goto node)
    {
        // We merge the current flow into the referenced section
        var labelInfo = this.GetInfo(node.Target);
        labelInfo.Meet(prev);
        return default;
    }

    private Unit Visit(FlowInfo prev, Ast.Expr.While node)
    {
        var continueInfo = this.GetInfo(node.ContinueLabel);
        var breakInfo = this.GetInfo(node.BreakLabel);

        // The info from continue meets into the current flow
        prev.Meet(continueInfo);

        // We run the condition
        this.Visit(prev, node.Condition);

        // Based on the condition, we have two alternative paths,
        // we either go straight to the break label, or go through the body, then meet into the continue label

        // First, the break
        breakInfo.Meet(prev);

        // Now, the body, and then meet into continue
        this.Visit(prev, node.Expression);
        continueInfo.Meet(prev);

        // Finally after the body, the flow can meet into the break label
        prev.Meet(breakInfo);

        return default;
    }

    private Unit Visit(FlowInfo prev, Ast.Expr.If node)
    {
        // The condition is always ran
        this.Visit(prev, node.Condition);

        // Now we either run then or else case, for that we clone the lattices
        // Note, that cloning won't track the info in backReferences, but that should not matter here
        var prevAlt = prev.Clone();

        // Run the two alternatives
        this.Visit(prev, node.Then);
        this.Visit(prevAlt, node.Else);

        // They can finally meet
        prev.Meet(prevAlt);

        return default;
    }

    private Unit Visit(FlowInfo prev, Ast.Expr.Block node)
    {
        // Blocks are not relevant for flow, we unroll them simply without any action
        foreach (var stmt in node.Statements) this.Visit(prev, stmt);
        this.Visit(prev, node.Value);

        return default;
    }

    private Unit Visit(FlowInfo prev, Ast.Decl.Variable node)
    {
        // The initializer happens before everything, if present
        if (node.Value is not null) this.Visit(prev, node.Value);
        // Now we can transfer on the variable declaration
        prev.Transfer(node);

        return default;
    }

    private Unit Visit(FlowInfo prev, Ast.Expr.Return node)
    {
        // Return value evaluated first
        this.Visit(prev, node.Expression);
        // Now we can transfer on the return itself
        prev.Transfer(node);

        return default;
    }
}
