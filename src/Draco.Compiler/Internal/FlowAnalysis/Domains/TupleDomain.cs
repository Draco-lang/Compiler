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
    public override bool Equals((T1, T2) state1, (T1, T2) state2) =>
        this.Domain1.Equals(state1.Item1, state2.Item1)
     && this.Domain2.Equals(state1.Item2, state2.Item2);

    public override string ToString((T1, T2) state) => $"({this.Domain1.ToString(state.Item1)}, {this.Domain2.ToString(state.Item2)})";

    public override void Join(ref (T1, T2) target, IEnumerable<(T1, T2)> sources)
    {
        this.Domain1.Join(ref target.Item1, sources.Select(s => s.Item1));
        this.Domain2.Join(ref target.Item2, sources.Select(s => s.Item2));
    }

    public override void Transfer(ref (T1, T2) state, BoundNode node)
    {
        this.Domain1.Transfer(ref state.Item1, node);
        this.Domain2.Transfer(ref state.Item2, node);
    }
}

/// <summary>
/// A tuple of domains for flow analysis.
/// </summary>
/// <typeparam name="T1">The type of the first domain.</typeparam>
/// <typeparam name="T2">The type of the second domain.</typeparam>
/// <typeparam name="T3">The type of the third domain.</typeparam>
internal sealed class TupleDomain<T1, T2, T3> : FlowDomain<(T1, T2, T3)>
{
    /// <summary>
    /// The first domain.
    /// </summary>
    public FlowDomain<T1> Domain1 { get; }

    /// <summary>
    /// The second domain.
    /// </summary>
    public FlowDomain<T2> Domain2 { get; }

    /// <summary>
    /// The third domain.
    /// </summary>
    public FlowDomain<T3> Domain3 { get; }

    public override (T1, T2, T3) Initial => (this.Domain1.Initial, this.Domain2.Initial, this.Domain3.Initial);
    public override (T1, T2, T3) Top => (this.Domain1.Top, this.Domain2.Top, this.Domain3.Top);
    public override FlowDirection Direction => this.Domain1.Direction;

    public TupleDomain(FlowDomain<T1> d1, FlowDomain<T2> d2, FlowDomain<T3> d3)
    {
        if (d1.Direction != d2.Direction) throw new ArgumentException("the domains must have the same flow direction");
        if (d1.Direction != d3.Direction) throw new ArgumentException("the domains must have the same flow direction");
        this.Domain1 = d1;
        this.Domain2 = d2;
        this.Domain3 = d3;
    }

    public override (T1, T2, T3) Clone(in (T1, T2, T3) state) =>
        (this.Domain1.Clone(state.Item1), this.Domain2.Clone(state.Item2), this.Domain3.Clone(state.Item3));
    public override bool Equals((T1, T2, T3) state1, (T1, T2, T3) state2) =>
        this.Domain1.Equals(state1.Item1, state2.Item1)
     && this.Domain2.Equals(state1.Item2, state2.Item2)
     && this.Domain3.Equals(state1.Item3, state2.Item3);

    public override string ToString((T1, T2, T3) state) =>
        $"({this.Domain1.ToString(state.Item1)}, {this.Domain2.ToString(state.Item2)}, {this.Domain3.ToString(state.Item3)})";

    public override void Join(ref (T1, T2, T3) target, IEnumerable<(T1, T2, T3)> sources)
    {
        this.Domain1.Join(ref target.Item1, sources.Select(s => s.Item1));
        this.Domain2.Join(ref target.Item2, sources.Select(s => s.Item2));
        this.Domain3.Join(ref target.Item3, sources.Select(s => s.Item3));
    }

    public override void Transfer(ref (T1, T2, T3) state, BoundNode node)
    {
        this.Domain1.Transfer(ref state.Item1, node);
        this.Domain2.Transfer(ref state.Item2, node);
        this.Domain3.Transfer(ref state.Item3, node);
    }
}
