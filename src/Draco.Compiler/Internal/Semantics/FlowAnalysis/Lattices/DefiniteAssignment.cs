using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis.Lattices;

/// <summary>
/// A lattice for checking if all variable uses happen after an assignment.
/// https://en.wikipedia.org/wiki/Definite_assignment_analysis
/// </summary>
internal sealed class DefiniteAssignment : ILattice<ImmutableDictionary<ISymbol.IVariable, DefiniteAssignment.Status>>
{
    private static IEqualityComparer<IReadOnlyDictionary<ISymbol.IVariable, Status>> DictionaryComparer =>
        DictionaryEqualityComparer<ISymbol.IVariable, Status>.Default;

    public enum Status
    {
        NotInitialized = 0,
        Initialized = 1,
    }

    public static DefiniteAssignment Instance { get; } = new();

    public FlowDirection Direction => FlowDirection.Forward;
    public ImmutableDictionary<ISymbol.IVariable, Status> Identity => ImmutableDictionary<ISymbol.IVariable, Status>.Empty;

    private DefiniteAssignment()
    {
    }

    public bool Equals(ImmutableDictionary<ISymbol.IVariable, Status>? x, ImmutableDictionary<ISymbol.IVariable, Status>? y) =>
        DictionaryComparer.Equals(x, y);
    public int GetHashCode(ImmutableDictionary<ISymbol.IVariable, Status> obj) =>
        DictionaryComparer.GetHashCode(obj);

    public ImmutableDictionary<ISymbol.IVariable, Status> Join(
        ImmutableDictionary<ISymbol.IVariable, Status> a,
        ImmutableDictionary<ISymbol.IVariable, Status> b)
    {
        var result = a.ToBuilder();

        foreach (var (sym, stat) in b)
        {
            if (a.TryGetValue(sym, out var existingStat) && (int)existingStat >= (int)stat) continue;
            result[sym] = stat;
        }

        return result.ToImmutable();
    }

    public ImmutableDictionary<ISymbol.IVariable, Status> Meet(
        ImmutableDictionary<ISymbol.IVariable, Status> a,
        ImmutableDictionary<ISymbol.IVariable, Status> b)
    {
        var result = a.ToBuilder();

        foreach (var (sym, stat) in b)
        {
            if (a.TryGetValue(sym, out var existingStat) && (int)existingStat <= (int)stat) continue;
            result[sym] = stat;
        }

        return result.ToImmutable();
    }

    public ImmutableDictionary<ISymbol.IVariable, Status> Transfer(Ast node) => node switch
    {
        Ast.Decl.Variable v when v.Value is not null => CreateDictionary(v.DeclarationSymbol, Status.Initialized),
        Ast.Decl.Variable v when v.Value is null => CreateDictionary(v.DeclarationSymbol, Status.NotInitialized),
        Ast.Expr.Assign a when a.Target is Ast.LValue.Reference r
                            && r.Symbol is ISymbol.IVariable v => CreateDictionary(v, Status.Initialized),
        _ => ImmutableDictionary<ISymbol.IVariable, Status>.Empty,
    };

    private static ImmutableDictionary<ISymbol.IVariable, Status> CreateDictionary(ISymbol.IVariable symbol, Status status) =>
        ImmutableDictionary.Create<ISymbol.IVariable, Status>().Add(symbol, status);
}
