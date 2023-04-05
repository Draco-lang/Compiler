using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.FlowAnalysis.Lattices;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Types;
using Draco.Compiler.Internal.Symbols.Source;
using System.Numerics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.FlowAnalysis;

// TODO: This is definitely not incremental
// We don't care for now, but later the flow graph construction and the passes should become incremental
// It should not be a big code-shift

/// <summary>
/// Accumulates all data-flow passes as one.
/// </summary>
internal sealed class DataFlowPasses : BoundTreeVisitor
{
    /// <summary>
    /// Performs all DFA analysis on the given bound tree.
    /// </summary>
    /// <param name="module">The module to perform the analysis on.</param>
    /// <returns>The list of <see cref="Diagnostic"/>s produced during analysis.</returns>
    public static ImmutableArray<Diagnostic> Analyze(SourceModuleSymbol module)
    {
        var passes = new DataFlowPasses();
        passes.AnalyzeModule(module);
        return passes.diagnostics.ToImmutable();
    }

    private readonly ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

    private DataFlowPasses()
    {
    }

    private void AnalyzeModule(SourceModuleSymbol module)
    {
        foreach (var symbol in module.Members)
        {
            if (symbol is SourceFunctionSymbol function) this.AnalyzeFunction(function);
            else if (symbol is SourceGlobalSymbol global) this.CheckIfGlobalValIsInitialized(global);
        }
    }

    private void AnalyzeFunction(SourceFunctionSymbol function)
    {
        var graph = BoundTreeToDataFlowGraph.ToDataFlowGraph(function.Body);

        this.CheckIfVariablesAreAssignedCorrectType(graph);
        this.CheckReturnsOnAllPaths(function, graph);
        this.CheckIfOnlyInitializedVariablesAreUsed(graph);

        function.Body.Accept(this);
    }

    public override void VisitLocalDeclaration(BoundLocalDeclaration node)
    {
        base.VisitLocalDeclaration(node);
        this.CheckIfLocalValIsInitialized(node);
    }

    public override void VisitAssignmentExpression(BoundAssignmentExpression node)
    {
        base.VisitAssignmentExpression(node);
        this.CheckIfValIsNotAssigned(node);
    }

    private void CheckIfLocalValIsInitialized(BoundLocalDeclaration node)
    {
        if (node.Local.IsMutable) return;
        if (node.Value is not null) return;

        // Not initialized
        this.diagnostics.Add(Diagnostic.Create(
            template: DataflowErrors.ImmutableVariableMustBeInitialized,
            location: node.Syntax?.Location,
            formatArgs: node.Local.Name));
    }

    // TODO: Copypasta, can we get rid of it?
    private void CheckIfGlobalValIsInitialized(SourceGlobalSymbol global)
    {
        if (global.IsMutable) return;
        if (global.Value is not null) return;

        // Not initialized
        this.diagnostics.Add(Diagnostic.Create(
            template: DataflowErrors.ImmutableVariableMustBeInitialized,
            location: global.DeclarationSyntax.Location,
            formatArgs: global.Name));
    }

    private void CheckIfValIsNotAssigned(BoundAssignmentExpression node)
    {
        var symbol = node.Left switch
        {
            BoundLocalLvalue local => local.Local as VariableSymbol,
            BoundGlobalLvalue global => global.Global,
            _ => null,
        };
        if (symbol is null) return;
        if (symbol.IsMutable) return;

        // Immutable and modified
        this.diagnostics.Add(Diagnostic.Create(
            template: DataflowErrors.ImmutableVariableCanNotBeAssignedTo,
            location: node.Syntax?.Location,
            formatArgs: symbol.Name));
    }

    private void CheckReturnsOnAllPaths(SourceFunctionSymbol function, DataFlowGraph graph)
    {
        // We check if all operations without a successor are a return
        var allReturns = graph.Operations
            .Where(op => op.Successors.Count == 0)
            .All(op => op.Node is BoundReturnExpression);
        if (!allReturns)
        {
            // Does not return on all paths
            this.diagnostics.Add(Diagnostic.Create(
                template: DataflowErrors.DoesNotReturn,
                location: function.DeclarationSyntax.Location,
                formatArgs: function.Name));
        }
    }

    private void CheckIfOnlyInitializedVariablesAreUsed(DataFlowGraph graph)
    {
        var infos = DataFlowAnalysis.Analyze(
            lattice: DefiniteAssignment.Instance,
            graph: graph);
        foreach (var (node, info) in infos)
        {
            // We only care about references that reference local variables
            if (node is not BoundLocalExpression localExpr) continue;

            var local = localExpr.Local;
            if (info.In[local] != DefiniteAssignment.Status.Initialized)
            {
                // Use of uninitialized variable
                this.diagnostics.Add(Diagnostic.Create(
                    template: DataflowErrors.VariableUsedBeforeInit,
                    location: node.Syntax?.Location,
                    formatArgs: local.Name));
            }
        }
    }

    private void CheckIfVariablesAreAssignedCorrectType(DataFlowGraph graph)
    {
        var exprs = graph.Operations.Where(x => x.Node is BoundAssignmentExpression || x.Node is BoundLocalDeclaration);
        foreach (var expr in exprs)
        {
            switch (expr.Node)
            {
            case BoundAssignmentExpression ex: this.CheckIfAssignmentHasCorrectType(ex); break;
            case BoundLocalDeclaration dec: this.CheckIfLocalDeclarationHasCorrectType(dec); break;
            default: throw new System.InvalidOperationException();
            }
        }
    }

    private void CheckIfLocalDeclarationHasCorrectType(BoundLocalDeclaration declaration)
    {
        if (declaration.Local.Type is not BuiltinType builtin
            || !builtin.Bases.Select(x => x.Name).Contains("integral")) return; // TODO: This is very ugly
        if (declaration.Value is BoundLiteralExpression lit && lit.Value is not null)
        {
            this.CheckIfValueIsCorrectType(builtin, lit.Value, declaration.Syntax);
            return;
        }
    }

    private void CheckIfAssignmentHasCorrectType(BoundAssignmentExpression expr)
    {
        if (expr.Left.Type is not BuiltinType builtin
            || !builtin.Bases.Select(x => x.Name).Contains("integral")) return; // TODO: This is very ugly
        if (expr.Right is BoundLiteralExpression lit && lit.Value is not null)
        {
            this.CheckIfValueIsCorrectType(builtin, lit.Value, expr.Syntax);
            return;
        }
    }

    private void CheckIfValueIsCorrectType(Type type, object value, SyntaxNode? node)
    {
        bool result = false;
        if (ReferenceEquals(type, IntrinsicTypes.Int8)) result = sbyte.TryParse(value.ToString(), out _);
        else if (ReferenceEquals(type, IntrinsicTypes.Int16)) result = short.TryParse(value.ToString(), out _);
        else if (ReferenceEquals(type, IntrinsicTypes.Int32)) result = int.TryParse(value.ToString(), out _);
        else if (ReferenceEquals(type, IntrinsicTypes.Int64)) result = long.TryParse(value.ToString(), out _);

        else if (ReferenceEquals(type, IntrinsicTypes.Uint8)) result = byte.TryParse(value.ToString(), out _);
        else if (ReferenceEquals(type, IntrinsicTypes.Uint16)) result = ushort.TryParse(value.ToString(), out _);
        else if (ReferenceEquals(type, IntrinsicTypes.Uint32)) result = uint.TryParse(value.ToString(), out _);
        else if (ReferenceEquals(type, IntrinsicTypes.Uint64)) result = ulong.TryParse(value.ToString(), out _);

        else if (ReferenceEquals(type, IntrinsicTypes.Float32)) result = float.TryParse(value.ToString(), out _);
        else if (ReferenceEquals(type, IntrinsicTypes.Float64)) result = double.TryParse(value.ToString(), out _);
        if (result == false) this.diagnostics.Add(Diagnostic.Create(
            template: DataflowErrors.ValueOutOfRangeOfType,
            location: node?.Location,
            formatArgs: (type as BuiltinType)!.Name));
    }

    //private Dictionary<Type, (object min, object max)> numericTypesRanges = new Dictionary<Type, (object min, object max)>()
    //{
    //    { IntrinsicTypes.Int8, (sbyte.MinValue, sbyte.MaxValue) },
    //    { IntrinsicTypes.Int16, (short.MinValue, short.MaxValue) },
    //    { IntrinsicTypes.Int32, (int.MinValue, int.MaxValue) },
    //    { IntrinsicTypes.Int64, (long.MinValue, long.MaxValue) },

    //    { IntrinsicTypes.Uint8, (byte.MinValue, byte.MaxValue) },
    //    { IntrinsicTypes.Uint16, (ushort.MinValue, ushort.MaxValue) },
    //    { IntrinsicTypes.Uint32, (uint.MinValue, uint.MaxValue) },
    //    { IntrinsicTypes.Uint64, (ulong.MinValue, ulong.MaxValue) },

    //    { IntrinsicTypes.Float32, (float.MinValue, float.MaxValue) },
    //    { IntrinsicTypes.Float64, (double.MinValue, double.MaxValue) },
    //};
}
