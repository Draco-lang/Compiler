using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Utilities;
using static Draco.Compiler.Internal.Syntax.Lexer;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Utilities for invoking data-flow analysis.
/// </summary>
internal static class DataFlowAnalysis
{
    /// <summary>
    /// Performs data-flow analysis on the given AST.
    /// </summary>
    /// <param name="lattice">The lattice to use for analysis.</param>
    /// <param name="ast">The AST subtree to perform the analysis over.</param>
    /// <returns>The AST nodes associated with their inferred information.</returns>
    public static ImmutableDictionary<Ast, DataFlowInfo<TElement>> Analyze<TElement>(ILattice<TElement> lattice, Ast ast) =>
        DataFlowAnalysis<TElement>.Analyze(lattice, ast);
}

/// <summary>
/// Stores information about the in and out state during data flow analysis.
/// </summary>
/// <typeparam name="TElement">The lattice element type.</typeparam>
internal sealed class DataFlowInfo<TElement>
{
    /// <summary>
    /// The input, before the corresponding element.
    /// </summary>
    public TElement In { get; set; }

    /// <summary>
    /// The output, after the corresponding element.
    /// </summary>
    public TElement Out { get; set; }

    public DataFlowInfo(TElement @in, TElement @out)
    {
        this.In = @in;
        this.Out = @out;
    }
}

/// <summary>
/// Performs data-flow analysis using a lattice.
/// </summary>
/// <typeparam name="TElement">The lattice element type.</typeparam>
internal sealed class DataFlowAnalysis<TElement>
{
    /// <summary>
    /// Performs data-flow analysis on the given AST.
    /// </summary>
    /// <param name="lattice">The lattice to use for analysis.</param>
    /// <param name="ast">The AST subtree to perform the analysis over.</param>
    /// <returns>The AST nodes associated with their inferred information.</returns>
    public static ImmutableDictionary<Ast, DataFlowInfo<TElement>> Analyze(ILattice<TElement> lattice, Ast ast)
    {
        var analyzer = new DataFlowAnalysis<TElement>(lattice);
        while (analyzer.Pass(ast)) ;
        return analyzer.info.ToImmutable();
    }

    private readonly ILattice<TElement> lattice;
    private readonly ImmutableDictionary<Ast, DataFlowInfo<TElement>>.Builder info =
        ImmutableDictionary.CreateBuilder<Ast, DataFlowInfo<TElement>>(ReferenceEqualityComparer.Instance);
    private bool hasChanged;

    private DataFlowAnalysis(ILattice<TElement> lattice)
    {
        this.lattice = lattice;
    }

    private FlowDirection Direction => this.lattice.Direction;
    private TElement Identity => this.lattice.Identity;
    private bool Equals(TElement a, TElement b) => this.lattice.Equals(a, b);
    private TElement Meet(TElement a, TElement b) => this.lattice.Meet(a, b);
    private TElement Join(TElement a, TElement b) => this.Direction == FlowDirection.Forward
        ? this.lattice.Join(a, b)
        : this.lattice.Join(b, a);

    private DataFlowInfo<TElement> GetInfo(Ast node)
    {
        if (!this.info.TryGetValue(node, out var info))
        {
            var passed = this.lattice.Transfer(node);
            info = this.Direction == FlowDirection.Forward
                ? new(@in: this.Identity, @out: passed)
                : new(@in: passed, @out: this.Identity);
            this.info.Add(node, info);
        }
        return info;
    }

    private bool UpdateInfo(DataFlowInfo<TElement> info, TElement @in, TElement @out)
    {
        var infoChanged = !this.Equals(@in, info.In) || !this.Equals(@out, info.Out);
        this.hasChanged = infoChanged || this.hasChanged;
        info.In = @in;
        info.Out = @out;
        return infoChanged;
    }

    private bool Pass(Ast node)
    {
        this.hasChanged = false;
        this.Visit(this.lattice.Identity, node);
        return this.hasChanged;
    }

    // NOTE: We are doing this, because 'Unit' is shared by reference, which means it would poison the whole flow
    // in case there are multiple units in the AST (which is extemely likely)
    private static bool IsIrrelevant(Ast node) => node
        is Ast.Expr.Unit
        or Ast.Expr.Literal;

    private TElement Visit(TElement prev, Ast node)
    {
        if (IsIrrelevant(node)) return prev;

        var info = this.GetInfo(node);
        if (this.Direction == FlowDirection.Forward)
        {
            var @in = this.Join(prev, info.In);
            var @out = this.Join(this.VisitImpl(@in, node), info.Out);
            this.UpdateInfo(info, @in, @out);
            return @out;
        }
        else
        {
            var @out = this.Join(prev, info.Out);
            var @in = this.Join(this.VisitImpl(@out, node), info.In);
            this.UpdateInfo(info, @in, @out);
            return @in;
        }
    }

    private TElement VisitImpl(TElement prev, Ast node) => node switch
    {
        Ast.Stmt.Expr n => this.Visit(prev, n.Expression),
        Ast.Stmt.Decl n => this.Visit(prev, n.Declaration),
        Ast.Decl.Variable n => this.VisitImpl(prev, n),
        Ast.Expr.Return n => this.VisitImpl(prev, n),
        Ast.Expr.Block n => this.VisitImpl(prev, n),
        Ast.Expr.If n => this.VisitImpl(prev, n),
        Ast.Expr.While n => this.VisitImpl(prev, n),
        Ast.Expr.Binary n => this.VisitImpl(prev, n),
        Ast.Expr.String n => this.VisitImpl(prev, n),
        // Keep it here so it gets an entry
        Ast.Expr.Reference => prev,
        // We can't infer any better
        Ast.Expr.Unexpected => prev,
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };

    private TElement VisitImpl(TElement prev, Ast.Decl.Variable node)
    {
        if (node.Value is not null) prev = this.Visit(prev, node.Value);
        return prev;
    }

    private TElement VisitImpl(TElement prev, Ast.Expr.Binary node)
    {
        if (this.Direction == FlowDirection.Forward)
        {
            prev = this.Visit(prev, node.Left);
            return this.Visit(prev, node.Right);
        }
        else
        {
            prev = this.Visit(prev, node.Right);
            return this.Visit(prev, node.Left);
        }
    }

    private TElement VisitImpl(TElement prev, Ast.Expr.String node)
    {
        if (this.Direction == FlowDirection.Forward)
        {
            // We only care about interpolated parts
            foreach (var part in node.Parts.OfType<Ast.StringPart.Expr>()) prev = this.Visit(prev, part);
        }
        else
        {
            // We only care about interpolated parts in reverse order
            foreach (var part in node.Parts.OfType<Ast.StringPart.Expr>().Reverse()) prev = this.Visit(prev, part);
        }
        return prev;
    }

    private TElement VisitImpl(TElement prev, Ast.Expr.Return node) =>
        this.Visit(prev, node.Expression);

    private TElement VisitImpl(TElement prev, Ast.Expr.Block node)
    {
        if (this.Direction == FlowDirection.Forward)
        {
            foreach (var stmt in node.Statements) prev = this.Visit(prev, stmt);
            return this.Visit(prev, node.Value);
        }
        else
        {
            prev = this.Visit(prev, node.Value);
            foreach (var stmt in node.Statements.Reverse()) prev = this.Visit(prev, stmt);
            return prev;
        }
    }

    private TElement VisitImpl(TElement prev, Ast.Expr.If node)
    {
        if (this.Direction == FlowDirection.Forward)
        {
            // Condition is always evaluated
            prev = this.Visit(prev, node.Condition);

            // Then there are two alternative futures, depending on which branch runs
            var thenPrev = this.Visit(prev, node.Then);
            var elsePrev = this.Visit(prev, node.Else);

            // Merge futures
            return this.Meet(thenPrev, elsePrev);
        }
        else
        {
            // Two alternative predecessors
            var thenPrev = this.Visit(prev, node.Then);
            var elsePrev = this.Visit(prev, node.Else);

            // Merge them
            prev = this.Meet(thenPrev, elsePrev);

            // Condition always runs
            return this.Visit(prev, node.Condition);
        }
    }

    private TElement VisitImpl(TElement prev, Ast.Expr.While node)
    {
    start:
        // NOTE: Since this is cyclic, the direction should not matter that much here
        var beforeCondition = prev;

        // Condition is always evaluated
        prev = this.Visit(prev, node.Condition);

        // Then there are two alternative futures, depending on if the while body runs
        var bodyRanPrev = this.Visit(prev, node.Expression);

        // Merge them
        prev = this.Meet(prev, bodyRanPrev);

        // Loop it back to before the condition
        var prevLoop = this.Join(prev, beforeCondition);
        if (!this.Equals(prevLoop, beforeCondition))
        {
            prev = prevLoop;
            goto start;
        }
        return prev;
    }
}
