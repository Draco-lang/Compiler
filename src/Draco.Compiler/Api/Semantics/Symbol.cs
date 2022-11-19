using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using InternalSymbol = Draco.Compiler.Internal.Semantics.Symbol;

namespace Draco.Compiler.Api.Semantics;

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

    internal InternalSymbol InternalSymbol { get; }

    public Symbol(InternalSymbol internalSymbol)
    {
        this.InternalSymbol = internalSymbol;
    }

    public override bool Equals(object? obj) => this.Equals(obj as ISymbol);

    public bool Equals(ISymbol? other) =>
           other is Symbol otherSym
        && object.Equals(this.InternalSymbol, otherSym.InternalSymbol);

    public override int GetHashCode() => this.InternalSymbol.GetHashCode();
}
