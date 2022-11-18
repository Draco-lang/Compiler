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
public interface ISymbol
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
    public string Name => this.internalSymbol.Name;
    public bool IsError => this.internalSymbol.IsError;
    public Location? Definition => this.internalSymbol.Definition?.Location;
    public ImmutableArray<Diagnostic> Diagnostics => throw new NotImplementedException();

    private readonly InternalSymbol internalSymbol;

    public Symbol(InternalSymbol internalSymbol)
    {
        this.internalSymbol = internalSymbol;
    }
}
