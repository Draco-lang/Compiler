using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Semantics;

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
/// Represents a single scope.
/// </summary>
/// <param name="Definition">The <see cref="ParseTree"/> that introduced this scope.</param>
/// <param name="Kind">The kind of scope.</param>
/// <param name="Timelines">The symbol names in this scope associated with their
/// <see cref="DeclarationTimeline"/>s.</param>
internal sealed record class Scope(
    ParseTree? Definition,
    ScopeKind Kind,
    ImmutableDictionary<string, DeclarationTimeline> Timelines)
{
    /// <summary>
    /// Attempts to look up a <see cref="Declaration"/> with a given name.
    /// </summary>
    /// <param name="name">The name of the <see cref="Declaration"/> to look for.</param>
    /// <param name="referencedPosition">The position we allow lookup up until.</param>
    /// <returns>The <see cref="Declaration"/> that has name <paramref name="name"/> and is visible from
    /// position <paramref name="referencedPosition"/>, or null if there is none such.</returns>
    public Declaration? LookUp(string name, int referencedPosition)
    {
        if (!this.Timelines.TryGetValue(name, out var timeline)) return null;
        return timeline.LookUp(referencedPosition);
    }
}

/// <summary>
/// Represents the timeline of <see cref="Symbol"/>s that are introduced in the same <see cref="Scope"/>
/// under the same name.
/// </summary>
internal readonly struct DeclarationTimeline
{
    /// <summary>
    /// The <see cref="Declaration"/>s that introduce the <see cref="Symbol"/>s.
    /// </summary>
    public readonly ImmutableArray<Declaration> Declarations;

    public DeclarationTimeline(IEnumerable<Declaration> declarations)
    {
        // Either there are no declarations, or all of them have the same name
        Debug.Assert(!declarations.Any()
                   || declarations.All(d => d.Name == declarations.First().Name));
        this.Declarations = declarations
            .OrderBy(decl => decl.Position)
            .ToImmutableArray();
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
/// Represents the declaration of a <see cref="Semantics.Symbol"/> in a <see cref="Scope"/>.
/// </summary>
/// <param name="Position">The relative position of the delcaration relative to the containing scope.
/// The position is where the symbol is available from.</param>
/// <param name="Symbol">The declared <see cref="Semantics.Symbol"/>.</param>
internal readonly record struct Declaration(int Position, Symbol Symbol)
{
    /// <summary>
    /// The name of the contained <see cref="Symbol"/>.
    /// </summary>
    public string Name => this.Symbol.Name;
}
