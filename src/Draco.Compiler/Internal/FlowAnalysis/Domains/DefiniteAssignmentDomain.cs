using System.Collections;
using System.Collections.Generic;
using System.Text;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.FlowAnalysis.Domains;

/// <summary>
/// A domain for performing definite assignment analysis.
/// </summary>
internal sealed class DefiniteAssignmentDomain(IEnumerable<LocalSymbol> locals)
    : GenKillFlowDomain<LocalSymbol>(locals)
{
    public override FlowDirection Direction => FlowDirection.Forward;

    public override BitArray Initial => new(this.Elements.Length, true);
    public override BitArray Top => new(this.Elements.Length, true);

    public override string ToString(BitArray state)
    {
        var result = new StringBuilder();
        result.Append('[');
        var first = true;
        for (var i = 0; i < this.Elements.Length; i++)
        {
            if (state[i]) continue;
            if (!first) result.Append(',');
            result.Append(this.Elements[i].Name);
            first = false;
        }
        result.Append(']');
        return result.ToString();
    }

    public override void Join(ref BitArray target, IEnumerable<BitArray> sources)
    {
        target.SetAll(false);
        foreach (var element in sources) target.Or(element);
    }

    protected override BitArray Gen(BoundNode node) => new(this.Elements.Length);
    protected override BitArray Kill(BoundNode node) => node switch
    {
        BoundAssignmentExpression assignment when assignment.Left is BoundLocalLvalue localLvalue =>
            this.CreateWithBitsSet([localLvalue.Local]).Not(),
        _ => new(this.Elements.Length),
    };
}
