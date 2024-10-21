using System.Collections;
using System.Collections.Generic;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.FlowAnalysis.Domains;

/// <summary>
/// A domain for checking if a variable is assigned only once or not.
/// </summary>
internal sealed class SingleAssignmentDomain(IEnumerable<LocalSymbol> locals)
    : GenKillFlowDomain<LocalSymbol>(locals)
{
    public override FlowDirection Direction => FlowDirection.Forward;
    public override BitArray Top => new(this.Elements.Length, false);

    public override void Join(ref BitArray target, IEnumerable<BitArray> sources)
    {
        target.SetAll(false);
        foreach (var source in sources) target.Or(source);
    }

    protected override BitArray Gen(BoundNode node) => node switch
    {
        BoundAssignmentExpression assign when assign.Left is BoundLocalLvalue local =>
            this.CreateWithBitsSet([local.Local]),
        _ => new(this.Elements.Length, false),
    };

    protected override BitArray Kill(BoundNode node) => new(this.Elements.Length, false);

    protected override string? ElementToString(BitArray state, int elementIndex) => state[elementIndex]
        ? this.Elements[elementIndex].Name
        : null;
}
