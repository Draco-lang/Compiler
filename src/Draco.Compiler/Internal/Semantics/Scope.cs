using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
/// <param name="Kind">The kind of scope.</param>
/// <param name="Symbols">The symbols within this scope.</param>
internal sealed record class Scope(
    ScopeKind Kind,
    ImmutableDictionary<string, Symbol> Symbols);
