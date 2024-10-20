using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.FlowAnalysis.Domains;

/// <summary>
/// A tuple of domains for flow analysis.
/// </summary>
/// <typeparam name="T1">The type of the first domain.</typeparam>
/// <typeparam name="T2">The type of the second domain.</typeparam>
internal sealed class TupleDomain<T1, T2> : FlowDomain<(T1, T2)>
{
    /// <summary>
    /// The first domain.
    /// </summary>
    public FlowDomain<T1> Domain1 { get; }

    /// <summary>
    /// The second domain.
    /// </summary>
    public FlowDomain<T2> Domain2 { get; }

    public override (T1, T2) Initial => (this.Domain1.Initial, this.Domain2.Initial);
    public override (T1, T2) Top => (this.Domain1.Top, this.Domain2.Top);
    public override FlowDirection Direction => this.Domain1.Direction;

    public TupleDomain(FlowDomain<T1> d1, FlowDomain<T2> d2)
    {
        if (d1.Direction != d2.Direction) throw new ArgumentException("the domains must have the same flow direction");
        this.Domain1 = d1;
        this.Domain2 = d2;
    }

    public override (T1, T2) Clone(in (T1, T2) state) => (this.Domain1.Clone(state.Item1), this.Domain2.Clone(state.Item2));
    public override string ToString((T1, T2) state) => $"({this.Domain1.ToString(state.Item1)}, {this.Domain2.ToString(state.Item2)})";

    public override void Join(ref (T1, T2) target, IEnumerable<(T1, T2)> sources)
    {
        var sources1 = sources.Select(s => s.Item1);
        var sources2 = sources.Select(s => s.Item2);
        this.Domain1.Join(ref target.Item1, sources1);
        this.Domain2.Join(ref target.Item2, sources2);
    }

    public override bool Transfer(ref (T1, T2) state, BoundNode node) =>
        this.Domain1.Transfer(ref state.Item1, node) | this.Domain2.Transfer(ref state.Item2, node);
}
