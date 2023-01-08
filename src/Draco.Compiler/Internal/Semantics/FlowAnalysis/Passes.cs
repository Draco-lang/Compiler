using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Semantics.FlowAnalysis.Lattices;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Accumulates all data-flow passes as one.
/// </summary>
internal sealed class DataFlowPasses : AstVisitorBase<Unit>
{
    /// <summary>
    /// Performs all DFA analysis on the given AST.
    /// </summary>
    /// <param name="ast">The AST to perform the analysis on.</param>
    /// <returns>The list of <see cref="Diagnostic"/>s produced during analysis.</returns>
    public static ImmutableArray<Diagnostic> Analyze(Ast ast)
    {
        var passes = new DataFlowPasses();
        passes.Visit(ast);
        return passes.diagnostics.ToImmutable();
    }

    private readonly ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

    private DataFlowPasses()
    {
    }

    public override Unit VisitFuncDecl(Ast.Decl.Func node)
    {
        this.CheckReturnsOnAllPaths(node);
        return this.Default;
    }

    private void CheckReturnsOnAllPaths(Ast.Decl.Func node)
    {
        var op = AstToDataFlowOperation.ToDataFlowOperation(node.Body);
        // TODO: Temporary
        Console.WriteLine(DataFlowOperationPrinter.ToDot(op));
        var infos = DataFlowAnalysis.Analyze(ReturnsOnAllPaths.Instance, op);
        var info = infos[op.Node];
        if (info.Out != ReturnsOnAllPaths.Status.Returns)
        {
            // Does not return on all paths
            this.diagnostics.Add(Diagnostic.Create(
                template: SemanticErrors.DoesNotReturn,
                location: node.DeclarationSymbol.Definition?.Location ?? Location.None,
                formatArgs: node.DeclarationSymbol.Name));
        }
    }
}
