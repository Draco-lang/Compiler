using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.FlowAnalysis.Lattices;

/// <summary>
/// A lattice for checking if all variable uses happen after an assignment.
/// https://en.wikipedia.org/wiki/Definite_assignment_analysis
/// </summary>
internal sealed class DefiniteAssignment : ILattice<ImmutableDictionary<LocalSymbol, DefiniteAssignment.Status>>
{
    private static IEqualityComparer<IReadOnlyDictionary<LocalSymbol, Status>> DictionaryComparer =>
        DictionaryEqualityComparer<LocalSymbol, Status>.Default;

    public enum Status
    {
        NotInitialized = 0,
        Initialized = 1,
    }

    public static DefiniteAssignment Instance { get; } = new();

    public FlowDirection Direction => FlowDirection.Forward;
    public ImmutableDictionary<LocalSymbol, Status> Identity => ImmutableDictionary<LocalSymbol, Status>.Empty;

    private DefiniteAssignment()
    {
    }

    public bool Equals(ImmutableDictionary<LocalSymbol, Status>? x, ImmutableDictionary<LocalSymbol, Status>? y) =>
        DictionaryComparer.Equals(x, y);
    public int GetHashCode(ImmutableDictionary<LocalSymbol, Status> obj) =>
        DictionaryComparer.GetHashCode(obj);

    public ImmutableDictionary<LocalSymbol, Status> Join(
        ImmutableDictionary<LocalSymbol, Status> a,
        ImmutableDictionary<LocalSymbol, Status> b)
    {
        var result = a.ToBuilder();

        foreach (var (sym, stat) in b)
        {
            if (a.TryGetValue(sym, out var existingStat) && (int)existingStat >= (int)stat) continue;
            result[sym] = stat;
        }

        return result.ToImmutable();
    }

    public ImmutableDictionary<LocalSymbol, Status> Meet(
        ImmutableDictionary<LocalSymbol, Status> a,
        ImmutableDictionary<LocalSymbol, Status> b)
    {
        var result = a.ToBuilder();

        foreach (var (sym, stat) in b)
        {
            if (a.TryGetValue(sym, out var existingStat) && (int)existingStat <= (int)stat) continue;
            result[sym] = stat;
        }

        return result.ToImmutable();
    }

    public ImmutableDictionary<LocalSymbol, Status> Transfer(BoundNode node) => node switch
    {
        BoundLocalDeclaration v when v.Value is not null => CreateDictionary(v.Local, Status.Initialized),
        BoundLocalDeclaration v when v.Value is null => CreateDictionary(v.Local, Status.NotInitialized),
        BoundAssignmentExpression a when a.Left is BoundLocalLvalue l => CreateDictionary(l.Local, Status.Initialized),
        _ => ImmutableDictionary<LocalSymbol, Status>.Empty,
    };

    private static ImmutableDictionary<LocalSymbol, Status> CreateDictionary(LocalSymbol symbol, Status status) =>
        ImmutableDictionary.Create<LocalSymbol, Status>().Add(symbol, status);
}
