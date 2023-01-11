using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.Symbols;

// Factory /////////////////////////////////////////////////////////////////////

internal static partial class Scope
{
    public static IScope Make(
        QueryDatabase db,
        ScopeKind kind,
        ParseNode definition,
        ImmutableDictionary<string, DeclarationTimeline> timelines,
        ImmutableDictionary<ParseNode, ISymbol> declarations) => kind switch
        {
            ScopeKind.Global => MakeGlobal(db, definition, timelines, declarations),
            ScopeKind.Function => MakeFunction(db, definition, timelines, declarations),
            ScopeKind.Local => MakeLocal(db, definition, timelines, declarations),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    public static IScope MakeGlobal(
        QueryDatabase db,
        ParseNode definition,
        ImmutableDictionary<string, DeclarationTimeline> timelines,
        ImmutableDictionary<ParseNode, ISymbol> declarations) =>
        new GlobalScope(db, definition, timelines, declarations);

    public static IScope MakeFunction(
        QueryDatabase db,
        ParseNode definition,
        ImmutableDictionary<string, DeclarationTimeline> timelines,
        ImmutableDictionary<ParseNode, ISymbol> declarations) =>
        new FunctionScope(db, definition, timelines, declarations);

    public static IScope MakeLocal(
        QueryDatabase db,
        ParseNode definition,
        ImmutableDictionary<string, DeclarationTimeline> timelines,
        ImmutableDictionary<ParseNode, ISymbol> declarations) =>
        new LocalScope(db, definition, timelines, declarations);
}

// Interfaces //////////////////////////////////////////////////////////////////

/// <summary>
/// The different kinds of scopes possible.
/// </summary>
internal enum ScopeKind
{
    /// <summary>
    /// Global scope.
    /// </summary>
    Global,

    /// <summary>
    /// A scope the function defines as its boundary.
    /// </summary>
    Function,

    /// <summary>
    /// Completely local scope.
    /// </summary>
    Local,
}

/// <summary>
/// The interface of all scopes.
/// </summary>
internal interface IScope
{
    /// <summary>
    /// The kind of this scope.
    /// </summary>
    public ScopeKind Kind { get; }

    /// <summary>
    /// The parent of this scope.
    /// </summary>
    public IScope? Parent { get; }

    /// <summary>
    /// The <see cref="ParseNode"/> that introduced this scope.
    /// </summary>
    public ParseNode? Definition { get; }

    /// <summary>
    /// The symbol names in this scope associated with their <see cref="DeclarationTimeline"/>s.
    /// </summary>
    public ImmutableDictionary<string, DeclarationTimeline> Timelines { get; }

    /// <summary>
    /// The symbols associated to their declaration.
    /// </summary>
    public ImmutableDictionary<ParseNode, ISymbol> Declarations { get; }

    /// <summary>
    /// True, if this is the global scope.
    /// </summary>
    public bool IsGlobal { get; }

    /// <summary>
    /// True, if this is a function scope.
    /// </summary>
    public bool IsFunction { get; }

    /// <summary>
    /// True, if this is a local scope.
    /// </summary>
    public bool IsLocal { get; }

    /// <summary>
    /// Attempts to look up a <see cref="ISymbol"/> with a given name, using a given predicate projection.
    /// </summary>
    /// <typeparam name="TSymbol">The symbol type the projection returns.</typeparam>
    /// <param name="name">The name of the <see cref="Declaration"/> to look for.</param>
    /// <param name="referencedPosition">The position we allow lookup up until.</param>
    /// <param name="projection">The projection to select the result with.</param>
    /// <returns>The <typeparamref name="TSymbol"/> that has name <paramref name="name"/>, is visible from
    /// position <paramref name="referencedPosition"/> and <paramref name="projection"/> didn't return default for it,
    /// or the default value, if there is none such.</returns>
    public TSymbol? LookUp<TSymbol>(string name, int referencedPosition, Func<ISymbol, TSymbol?> projection)
        where TSymbol : ISymbol;
}

// Implementations /////////////////////////////////////////////////////////////

internal static partial class Scope
{
    /// <summary>
    /// Base class for all scopes.
    /// </summary>
    private abstract class ScopeBase : IScope
    {
        public abstract ScopeKind Kind { get; }
        public bool IsGlobal => this.Kind == ScopeKind.Global;
        public bool IsFunction => this.Kind == ScopeKind.Function;
        public bool IsLocal => this.Kind == ScopeKind.Local;
        public IScope? Parent => SymbolResolution.GetParentScopeOrNull(this.db, this);
        public ParseNode Definition { get; }
        public ImmutableDictionary<string, DeclarationTimeline> Timelines { get; }
        public ImmutableDictionary<ParseNode, ISymbol> Declarations { get; }

        private readonly QueryDatabase db;

        protected ScopeBase(
            QueryDatabase db,
            ParseNode definition,
            ImmutableDictionary<string, DeclarationTimeline> timelines,
            ImmutableDictionary<ParseNode, ISymbol> declarations)
        {
            this.db = db;
            this.Definition = definition;
            this.Timelines = timelines;
            this.Declarations = declarations;
        }

        public TSymbol? LookUp<TSymbol>(string name, int referencedPosition, Func<ISymbol, TSymbol?> projection)
            where TSymbol : ISymbol
        {
            if (!this.Timelines.TryGetValue(name, out var timeline)) return default;
            return timeline.LookUp(referencedPosition, projection);
        }
    }
}

internal static partial class Scope
{
    /// <summary>
    /// Global scope.
    /// </summary>
    private sealed class GlobalScope : ScopeBase
    {
        public override ScopeKind Kind => ScopeKind.Global;

        public GlobalScope(
            QueryDatabase db,
            ParseNode definition,
            ImmutableDictionary<string, DeclarationTimeline> timelines,
            ImmutableDictionary<ParseNode, ISymbol> declarations)
            : base(db, definition, timelines, declarations)
        {
        }
    }
}

internal static partial class Scope
{
    /// <summary>
    /// Function scope.
    /// </summary>
    private sealed class FunctionScope : ScopeBase
    {
        public override ScopeKind Kind => ScopeKind.Function;

        public FunctionScope(
            QueryDatabase db,
            ParseNode definition,
            ImmutableDictionary<string, DeclarationTimeline> timelines,
            ImmutableDictionary<ParseNode, ISymbol> declarations)
            : base(db, definition, timelines, declarations)
        {
        }
    }
}

internal static partial class Scope
{
    /// <summary>
    /// Local scope.
    /// </summary>
    private sealed class LocalScope : ScopeBase
    {
        public override ScopeKind Kind => ScopeKind.Local;

        public LocalScope(
            QueryDatabase db,
            ParseNode definition,
            ImmutableDictionary<string, DeclarationTimeline> timelines,
            ImmutableDictionary<ParseNode, ISymbol> declarations)
            : base(db, definition, timelines, declarations)
        {
        }
    }
}

/// <summary>
/// Represents the timeline of <see cref="ISymbol"/>s that are introduced in the same <see cref="IScope"/>
/// under the same name.
/// </summary>
internal readonly struct DeclarationTimeline
{
    /// <summary>
    /// The <see cref="Declaration"/>s that introduce the <see cref="Symbol"/>s.
    /// </summary>
    public readonly ImmutableArray<Declaration> Declarations;

    public DeclarationTimeline(ImmutableArray<Declaration> declarations)
    {
        // Either there are no declarations, or all of them have the same name
        // They also must be ordered
        Debug.Assert(!declarations.Any()
                   || declarations.All(d => d.Name == declarations.First().Name));
        Debug.Assert(declarations.Select(d => d.Position).IsOrdered());

        this.Declarations = declarations;
    }

    /// <summary>
    /// Looks up a <see cref="ISymbol"/> in this timeline using a predicate projection.
    /// </summary>
    /// <typeparam name="TSymbol">The symbol type returned by the projection.</typeparam>
    /// <param name="referencedPosition">The position we are trying to reference in the timeline.</param>
    /// <returns>The <typeparamref name="TSymbol"/> that is the latest, but at most at
    /// <paramref name="referencedPosition"/> and <paramref name="projection"/> returned a non-default value for it,
    /// or null if there is no such declaration.</returns>
    public TSymbol? LookUp<TSymbol>(int referencedPosition, Func<ISymbol, TSymbol?> projection)
    {
        var comparer = Comparer<Declaration>.Create((d1, d2) => d1.Position - d2.Position);
        var searchKey = new Declaration(referencedPosition, null!);
        var index = this.Declarations.BinarySearch(searchKey, comparer);
        index = index >= 0
            // Exact position
            ? index
            // We are in-between, step one back
            : ~index - 1;
        for (var i = index; i >= 0; --i)
        {
            // Project
            var projected = projection(this.Declarations[i].Symbol);
            // If not null, found
            if (projected is not null) return projected;
        }
        // Not found in this timeline
        return default;
    }
}

/// <summary>
/// Represents the declaration of a <see cref="ISymbol"/> in a <see cref="IScope"/>.
/// </summary>
/// <param name="Position">The relative position of the delcaration relative to the containing scope.
/// The position is where the symbol is available from.</param>
/// <param name="Symbol">The declared <see cref="ISymbol"/>.</param>
internal readonly record struct Declaration(int Position, ISymbol Symbol)
{
    /// <summary>
    /// The name of the contained <see cref="ISymbol"/>.
    /// </summary>
    public string Name => this.Symbol.Name;

    /// <summary>
    /// The definition syntax.
    /// </summary>
    public ParseNode? Definition => this.Symbol.Definition;
}

internal static partial class Scope
{
    /// <summary>
    /// A builder type for constructing scopes.
    /// </summary>
    public sealed class Builder
    {
        public ScopeKind Kind { get; }
        public ParseNode Tree { get; }

        private readonly QueryDatabase db;
        private readonly Dictionary<string, ImmutableArray<Declaration>.Builder> declarations = new();

        public Builder(QueryDatabase db, ScopeKind kind, ParseNode tree)
        {
            this.db = db;
            this.Kind = kind;
            this.Tree = tree;
        }

        public void Add(Declaration declaration)
        {
            if (!this.declarations.TryGetValue(declaration.Name, out var timelineList))
            {
                timelineList = ImmutableArray.CreateBuilder<Declaration>();
                this.declarations.Add(declaration.Name, timelineList);
            }
            timelineList.Add(declaration);
        }

        public IScope Build()
        {
            var timelines = ImmutableDictionary.CreateBuilder<string, DeclarationTimeline>();
            var declarations = ImmutableDictionary.CreateBuilder<ParseNode, ISymbol>();

            void AddDeclaration(ISymbol symbol)
            {
                if (symbol.Definition is not null) declarations!.Add(symbol.Definition, symbol);
            }

            foreach (var (name, declsWithName) in this.declarations)
            {
                // Declarations must be in increasing order
                Debug.Assert(declsWithName.Select(decl => decl.Position).IsOrdered());
                var currentTimeline = ImmutableArray.CreateBuilder<Declaration>();
                foreach (var declsAtSamePosition in declsWithName
                    .GroupBy(decl => decl.Position)
                    .Select(g => g.ToList()))
                {
                    // The declarations are under the same name and same position
                    // The first symbol will determine how we handle the group
                    var first = declsAtSamePosition[0];
                    if (first.Symbol is ISymbol.IFunction)
                    {
                        // Potential overload set
                        var overloadSet = ImmutableArray.CreateBuilder<ISymbol.IFunction>();
                        foreach (var decl in declsAtSamePosition)
                        {
                            if (decl.Symbol is ISymbol.IFunction func)
                            {
                                // Part of the set
                                overloadSet.Add(func);
                                AddDeclaration(func);
                            }
                            else
                            {
                                // TODO: Provide other definitions position?
                                // Error, wrap it up
                                var diag = Diagnostic.Create(
                                    template: SymbolResolutionErrors.IllegalShadowing,
                                    location: decl.Definition is null ? Location.None : new Location.TreeReference(decl.Definition),
                                    formatArgs: name);
                                var errorSymbol = decl.Symbol.WithDiagnostics(ImmutableArray.Create(diag));
                                AddDeclaration(errorSymbol);
                            }
                        }
                        // Look in parent scope if there is an overload set
                        var symbolInParent = this.declarations.Values
                            .SelectMany(d => d)
                            .Select(d => d.Definition?.Parent)
                            .Where(t => t is not null)
                            .Select(t => SymbolResolution.ReferenceSymbolOrNull<ISymbol>(this.db, t!, name))
                            .FirstOrDefault();
                        if (symbolInParent is ISymbol.IFunction f)
                        {
                            // Parent has a function, add to overloads
                            overloadSet.Add(f);
                        }
                        else if (symbolInParent is ISymbol.IOverloadSet ovSet)
                        {
                            // Parent has an overload set, add all
                            overloadSet.AddRange(ovSet.Functions);
                        }
                        // Unwrap singular overload
                        var resultSymbol = overloadSet.Count == 1
                            ? overloadSet[0] as ISymbol
                            : Symbol.SynthetizeOverloadSet(overloadSet.ToImmutable());
                        // Add to timeline
                        currentTimeline.Add(new(Position: first.Position, resultSymbol));
                    }
                    else
                    {
                        // Only the first one is valid, the rest are wrapped up as error
                        currentTimeline.Add(first);
                        AddDeclaration(first.Symbol);
                        // Add rest as error
                        for (var i = 1; i < declsAtSamePosition.Count; ++i)
                        {
                            var other = declsAtSamePosition[i];
                            // TODO: Provide other definitions position?
                            // Error, wrap it up
                            var diag = Diagnostic.Create(
                                template: SymbolResolutionErrors.IllegalShadowing,
                                location: other.Definition is null ? Location.None : new Location.TreeReference(other.Definition),
                                formatArgs: name);
                            var errorSymbol = other.Symbol.WithDiagnostics(ImmutableArray.Create(diag));
                            AddDeclaration(errorSymbol);
                        }
                    }
                }
                timelines.Add(name, new(currentTimeline.ToImmutable()));
            }

            return Make(
                db: this.db,
                kind: this.Kind,
                definition: this.Tree,
                timelines: timelines.ToImmutable(),
                declarations: declarations.ToImmutable());
        }
    }
}
