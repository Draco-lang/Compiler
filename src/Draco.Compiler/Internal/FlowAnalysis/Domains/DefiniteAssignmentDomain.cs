using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.FlowAnalysis.Domains;

/// <summary>
/// A domain for performing definite assignment analysis.
/// Essentially checks, if a local variable is definitely assigned at a given point in the program.
/// </summary>
internal sealed class DefiniteAssignmentDomain(IEnumerable<LocalSymbol> locals)
    : GenKillFlowDomain<LocalSymbol>(locals)
{
    public override FlowDirection Direction => FlowDirection.Forward;

    public override BitArray Top => new(this.Elements.Length, false);
    public override BitArray Initial => new(this.Elements.Select(local => !IsForLoopVariable(local)).ToArray());

    public override void Join(ref BitArray target, IEnumerable<BitArray> sources)
    {
        target.SetAll(false);
        foreach (var source in sources) target.Or(source);
    }

    protected override BitArray Gen(BoundNode node) => new(this.Elements.Length, false);
    protected override BitArray Kill(BoundNode node) => node switch
    {
        BoundAssignmentExpression assign when assign.Left is BoundLocalLvalue local =>
            this.CreateWithBitsSet([local.Local]),
        _ => new(this.Elements.Length, false),
    };

    protected override string? ElementToString(BitArray state, int elementIndex) => state[elementIndex]
        ? null
        : this.Elements[elementIndex].Name;

    private static bool IsForLoopVariable(LocalSymbol local) =>
        local.DeclaringSyntax?.Parent is ForExpressionSyntax;
}
