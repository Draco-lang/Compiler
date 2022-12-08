using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using IInternalSymbol = Draco.Compiler.Internal.Semantics.Symbols.ISymbol;

namespace Draco.Compiler.Api.Semantics;

// NOTE: Eventually we'll need separate interfaces for each kind of symbol
// For now public API is not that big of a concern, so this is fine

/// <summary>
/// Represents a symbol in the language.
/// </summary>
public interface ISymbol : IEquatable<ISymbol>
{
    /// <summary>
    /// The name of the symbol.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// True, if this symbol represents an error.
    /// </summary>
    public bool IsError { get; }

    /// <summary>
    /// The list of <see cref="Diagnostic"/> messages attached to this symbol.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// The location where this symbol was defined.
    /// </summary>
    public Location? Definition { get; }
}

/// <summary>
/// Implementation for the symbol interfaces.
/// </summary>
internal sealed class Symbol : ISymbol
{
    public string Name => this.InternalSymbol.Name;
    public bool IsError => this.InternalSymbol.IsError;
    public Location? Definition => this.InternalSymbol.Definition?.Location;
    private ImmutableArray<Diagnostic>? diagnostics;
    public ImmutableArray<Diagnostic> Diagnostics =>
        this.diagnostics ??= this.InternalSymbol.Diagnostics
            .Select(diag => diag.ToApiDiagnostic(null))
            .ToImmutableArray();

    internal IInternalSymbol InternalSymbol { get; }

    public Symbol(IInternalSymbol internalSymbol)
    {
        this.InternalSymbol = internalSymbol;
    }

    public override bool Equals(object? obj) => this.Equals(obj as ISymbol);

    public bool Equals(ISymbol? other) =>
           other is Symbol otherSym
        && Equals(this.InternalSymbol, otherSym.InternalSymbol);

    public override int GetHashCode() => this.InternalSymbol.GetHashCode();
}
