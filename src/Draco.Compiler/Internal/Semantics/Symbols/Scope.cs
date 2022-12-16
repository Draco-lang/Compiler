using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.Symbols;

// Factory /////////////////////////////////////////////////////////////////////

internal partial interface IScope
{
    public static IScope Make(
        QueryDatabase db,
        ScopeKind kind,
        ParseTree definition,
        ImmutableDictionary<string, DeclarationTimeline> timelines) => kind switch
        {
            ScopeKind.Global => MakeGlobal(db, definition, timelines),
            ScopeKind.Function => MakeFunction(db, definition, timelines),
            ScopeKind.Local => MakeLocal(db, definition, timelines),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    public static IScope MakeGlobal(
        QueryDatabase db,
        ParseTree definition,
        ImmutableDictionary<string, DeclarationTimeline> timelines) =>
        new GlobalScope(db, definition, timelines);

    public static IScope MakeFunction(
        QueryDatabase db,
        ParseTree definition,
        ImmutableDictionary<string, DeclarationTimeline> timelines) =>
        new FunctionScope(db, definition, timelines);

    public static IScope MakeLocal(
        QueryDatabase db,
        ParseTree definition,
        ImmutableDictionary<string, DeclarationTimeline> timelines) =>
        new LocalScope(db, definition, timelines);
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
internal partial interface IScope
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
    /// The <see cref="ParseTree"/> that introduced this scope.
    /// </summary>
    public ParseTree? Definition { get; }

    /// <summary>
    /// The symbol names in this scope associated with their <see cref="DeclarationTimeline"/>s.
    /// </summary>
    public ImmutableDictionary<string, DeclarationTimeline> Timelines { get; }

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
    /// Attempts to look up a <see cref="Declaration"/> with a given name.
    /// </summary>
    /// <param name="name">The name of the <see cref="Declaration"/> to look for.</param>
    /// <param name="referencedPosition">The position we allow lookup up until.</param>
    /// <returns>The <see cref="Declaration"/> that has name <paramref name="name"/> and is visible from
    /// position <paramref name="referencedPosition"/>, or null if there is none such.</returns>
    public Declaration? LookUp(string name, int referencedPosition);
}

// Implementations /////////////////////////////////////////////////////////////

internal partial interface IScope
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
        public ParseTree Definition { get; }
        public ImmutableDictionary<string, DeclarationTimeline> Timelines { get; }

        private readonly QueryDatabase db;

        protected ScopeBase(
            QueryDatabase db,
            ParseTree definition,
            ImmutableDictionary<string, DeclarationTimeline> timelines)
        {
            this.db = db;
            this.Definition = definition;
            this.Timelines = timelines;
        }

        public Declaration? LookUp(string name, int referencedPosition)
        {
            if (!this.Timelines.TryGetValue(name, out var timeline)) return null;
            return timeline.LookUp(referencedPosition);
        }
    }
}

internal partial interface IScope
{
    /// <summary>
    /// Global scope.
    /// </summary>
    private sealed class GlobalScope : ScopeBase
    {
        public override ScopeKind Kind => ScopeKind.Global;

        public GlobalScope(
            QueryDatabase db,
            ParseTree definition,
            ImmutableDictionary<string, DeclarationTimeline> timelines)
            : base(db, definition, timelines)
        {
        }
    }
}

internal partial interface IScope
{
    /// <summary>
    /// Function scope.
    /// </summary>
    private sealed class FunctionScope : ScopeBase
    {
        public override ScopeKind Kind => ScopeKind.Function;

        public FunctionScope(
            QueryDatabase db,
            ParseTree definition,
            ImmutableDictionary<string, DeclarationTimeline> timelines)
            : base(db, definition, timelines)
        {
        }
    }
}

internal partial interface IScope
{
    /// <summary>
    /// Local scope.
    /// </summary>
    private sealed class LocalScope : ScopeBase
    {
        public override ScopeKind Kind => ScopeKind.Local;

        public LocalScope(
            QueryDatabase db,
            ParseTree definition,
            ImmutableDictionary<string, DeclarationTimeline> timelines)
            : base(db, definition, timelines)
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
    /// Looks up a <see cref="Declaration"/> in this timeline.
    /// </summary>
    /// <param name="referencedPosition">The position we are trying to reference in the timeline.</param>
    /// <returns>The <see cref="Declaration"/> that is the latest, but at most at
    /// <paramref name="referencedPosition"/>, or null if there is none such declaration.</returns>
    public Declaration? LookUp(int referencedPosition)
    {
        var comparer = Comparer<Declaration>.Create((d1, d2) => d1.Position - d2.Position);
        var searchKey = new Declaration(referencedPosition, null!);
        var index = this.Declarations.BinarySearch(searchKey, comparer);
        if (index >= 0)
        {
            // Exact match, can reference
            return this.Declarations[index];
        }
        else
        {
            // We are in-between, we need to get the previous one, which is defined
            index = ~index - 1;
            // Not found
            if (index < 0) return null;
            // Found one
            return this.Declarations[index];
        }
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
    /// The name of the contained <see cref="Symbol"/>.
    /// </summary>
    public string Name => this.Symbol.Name;
}
