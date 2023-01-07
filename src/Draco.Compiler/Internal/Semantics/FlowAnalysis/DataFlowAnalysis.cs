using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

    private bool Pass(Ast node)
    {
        this.hasChanged = false;
        this.Visit(this.lattice.Identity, node);
        return this.hasChanged;
    }

    private TElement Visit(TElement prev, Ast node)
    {
        var info = this.GetInfo(node);
        if (this.Direction == FlowDirection.Forward)
        {
            var @in = this.Join(prev, info.In);
            var @out = this.Join(this.VisitImpl(@in, node), info.Out);
            this.hasChanged = !Equals(@in, info.In) || !Equals(@out, info.Out) || this.hasChanged;
            info.In = @in;
            info.Out = @out;
            return @out;
        }
        else
        {
            var @out = this.Join(prev, info.Out);
            var @in = this.Join(this.VisitImpl(@out, node), info.In);
            this.hasChanged = !Equals(@in, info.In) || !Equals(@out, info.Out) || this.hasChanged;
            info.In = @in;
            info.Out = @out;
            return @in;
        }
    }

    private TElement VisitImpl(TElement prev, Ast node) => node switch
    {
        Ast.Stmt.Expr n => this.Visit(prev, n.Expression),
        Ast.Expr.Return n => this.VisitImpl(prev, n),
        Ast.Expr.Block n => this.VisitImpl(prev, n),
        Ast.Expr.Unit or Ast.Expr.Literal => prev,
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };

    private TElement VisitImpl(TElement prev, Ast.Expr.Return node) =>
        this.Visit(prev, node.Expression);

    private TElement VisitImpl(TElement prev, Ast.Expr.Block node)
    {
        foreach (var stmt in node.Statements) prev = this.Visit(prev, stmt);
        return this.Visit(prev, node.Value);
    }
}
