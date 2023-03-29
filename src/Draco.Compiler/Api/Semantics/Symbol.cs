using System;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Api.Semantics;

// Interfaces //////////////////////////////////////////////////////////////////

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
    /// The location where this symbol was defined.
    /// </summary>
    public Location? Definition { get; }

    /// <summary>
    /// Documentation attached to this symbol.
    /// </summary>
    public string Documentation { get; }
}

/// <summary>
/// Represents a variable symbol.
/// </summary>
public interface IVariableSymbol : ISymbol
{
    /// <summary>
    /// True, if this is a mutable variable.
    /// </summary>
    public bool IsMutable { get; }
}

/// <summary>
/// Represents a local variable symbol.
/// </summary>
public interface ILocalSymbol : IVariableSymbol
{
}

/// <summary>
/// Represents a global variable symbol.
/// </summary>
public interface IGlobalSymbol : IVariableSymbol
{
}

/// <summary>
/// Represents a parameter symbol.
/// </summary>
public interface IParameterSymbol : IVariableSymbol
{
}

/// <summary>
/// Represents a parameter symbol.
/// </summary>
public interface IFunctionSymbol : ISymbol
{
}

/// <summary>
/// Represents a type symbol.
/// </summary>
public interface ITypeSymbol : ISymbol
{
    /// <summary>
    /// The type this symbol represents.
    /// </summary>
    public IType Type { get; }
}

/// <summary>
/// Represents a label symbol.
/// </summary>
public interface ILabelSymbol : ISymbol
{
}

// Base classes ////////////////////////////////////////////////////////////////

internal abstract class SymbolBase : ISymbol
{
    public Symbol Symbol { get; }

    public string Name => this.Symbol.Name;
    public bool IsError => this.Symbol.IsError;
    public Location? Definition => this.Symbol.DeclarationSyntax?.Location;
    public string Documentation => this.Symbol.Documentation;

    public SymbolBase(Symbol symbol)
    {
        this.Symbol = symbol;
    }

    public bool Equals(ISymbol? other) => other is SymbolBase o
                                       && ReferenceEquals(this.Symbol, o.Symbol);

    public override int GetHashCode() => this.Symbol.GetHashCode();
}

internal abstract class SymbolBase<TInternalSymbol> : SymbolBase
    where TInternalSymbol : Symbol
{
    public new TInternalSymbol Symbol => (TInternalSymbol)base.Symbol;

    protected SymbolBase(TInternalSymbol symbol)
        : base(symbol)
    {
    }
}

// Proxy classes ///////////////////////////////////////////////////////////////

internal sealed class GlobalSymbol : SymbolBase<Internal.Symbols.GlobalSymbol>, IGlobalSymbol
{
    public bool IsMutable => this.Symbol.IsMutable;

    public GlobalSymbol(Internal.Symbols.GlobalSymbol global)
        : base(global)
    {
    }
}

internal sealed class LocalSymbol : SymbolBase<Internal.Symbols.LocalSymbol>, ILocalSymbol
{
    public bool IsMutable => this.Symbol.IsMutable;

    public LocalSymbol(Internal.Symbols.LocalSymbol local)
        : base(local)
    {
    }
}

internal sealed class ParameterSymbol : SymbolBase<Internal.Symbols.ParameterSymbol>, IParameterSymbol
{
    public bool IsMutable => this.Symbol.IsMutable;

    public ParameterSymbol(Internal.Symbols.ParameterSymbol parameter)
        : base(parameter)
    {
    }
}

internal sealed class FunctionSymbol : SymbolBase<Internal.Symbols.FunctionSymbol>, IFunctionSymbol
{
    public FunctionSymbol(Internal.Symbols.FunctionSymbol function)
        : base(function)
    {
    }
}

internal sealed class LabelSymbol : SymbolBase<Internal.Symbols.LabelSymbol>, ILabelSymbol
{
    public LabelSymbol(Internal.Symbols.LabelSymbol label)
        : base(label)
    {
    }
}

internal sealed class TypeSymbol : SymbolBase<Internal.Symbols.TypeSymbol>, ITypeSymbol
{
    public IType Type => this.Symbol.Type.ToApiType();

    public TypeSymbol(Internal.Symbols.TypeSymbol type)
        : base(type)
    {
    }
}

// NOTE: Mostly for generic error sentinel values
internal sealed class AnySymbol : SymbolBase<Internal.Symbols.Symbol>
{
    public AnySymbol(Internal.Symbols.Symbol type)
        : base(type)
    {
    }
}
