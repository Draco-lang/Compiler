using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Api.Semantics;

// Interfaces //////////////////////////////////////////////////////////////////

// TODO: Kill the "IEquatable" and expose a symbol equality comparer

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
    /// True, if this symbol represents special symbol.
    /// </summary>
    public bool IsSpecialName { get; }

    /// <summary>
    /// The location where this symbol was defined.
    /// </summary>
    public Location? Definition { get; }

    /// <summary>
    /// Documentation attached to this symbol.
    /// </summary>
    public string Documentation { get; }

    /// <summary>
    /// All the members within this symbol.
    /// </summary>
    public IEnumerable<ISymbol> Members { get; }

    /// <summary>
    /// The static members within this symbol.
    /// </summary>
    public IEnumerable<ISymbol> StaticMembers => this.Members.Where(x => x is IMemberSymbol mem && mem.IsStatic);

    /// <summary>
    /// The instance members within this symbol.
    /// </summary>
    public IEnumerable<ISymbol> InstanceMembers => this.Members.Where(x => x is IMemberSymbol mem && !mem.IsStatic);
}

/// <summary>
/// Represents a symbol that has type.
/// </summary>
public interface ITypedSymbol
{
    /// <summary>
    /// The type of this symbol.
    /// </summary>
    public ITypeSymbol Type { get; }
}

/// <summary>
/// Represents any member symbol.
/// </summary>
public interface IMemberSymbol
{
    /// <summary>
    /// Specifying if given symbol is static.
    /// </summary>
    public bool IsStatic { get; }
}


/// <summary>
/// Represents a module symbol.
/// </summary>
public interface IModuleSymbol : ISymbol, IMemberSymbol
{
}

/// <summary>
/// Represents a variable symbol.
/// </summary>
public interface IVariableSymbol : ISymbol, ITypedSymbol
{
    /// <summary>
    /// True, if this is a mutable variable.
    /// </summary>
    public bool IsMutable { get; }
}

/// <summary>
/// Represents a field symbol.
/// </summary>
public interface IFieldSymbol : IVariableSymbol, IMemberSymbol
{
}

/// <summary>
/// Represents a property symbol.
/// </summary>
public interface IPropertySymbol : IVariableSymbol, IMemberSymbol
{
    public IFunctionSymbol? Getter { get; }
    public IFunctionSymbol? Setter { get; }
}

/// <summary>
/// Represents a global variable symbol.
/// </summary>
public interface IGlobalSymbol : IVariableSymbol, IMemberSymbol
{
}


/// <summary>
/// Represents a local variable symbol.
/// </summary>
public interface ILocalSymbol : IVariableSymbol
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
public interface IFunctionSymbol : ISymbol, ITypedSymbol, IMemberSymbol
{
    /// <summary>
    /// The parameters this function defines.
    /// </summary>
    public ImmutableArray<IParameterSymbol> Parameters { get; }

    public ITypeSymbol ReturnType { get; }
}

/// <summary>
/// Represents a type symbol.
/// </summary>
public interface ITypeSymbol : ISymbol, IMemberSymbol
{
}

/// <summary>
/// Represents a type parameter symbol.
/// </summary>
public interface ITypeParameterSymbol : ITypeSymbol
{
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
    public bool IsSpecialName => this.Symbol.IsSpecialName;
    public Location? Definition => this.Symbol.DeclaringSyntax?.Location;
    public string Documentation => this.Symbol.Documentation;
    public IEnumerable<ISymbol> Members => this.Symbol.Members.Select(x => x.ToApiSymbol());

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

    public override string ToString() => this.Symbol.ToString();
}

// Proxy classes ///////////////////////////////////////////////////////////////

internal sealed class ModuleSymbol : SymbolBase<Internal.Symbols.ModuleSymbol>, IModuleSymbol
{
    public bool IsStatic => this.Symbol.IsStatic;

    public ModuleSymbol(Internal.Symbols.ModuleSymbol module)
        : base(module)
    {
    }
}

internal sealed class FieldSymbol : SymbolBase<Internal.Symbols.FieldSymbol>, IFieldSymbol
{
    public bool IsMutable => this.Symbol.IsMutable;
    public bool IsStatic => this.Symbol.IsStatic;
    public ITypeSymbol Type => this.Symbol.Type.ToApiSymbol();

    public FieldSymbol(Internal.Symbols.FieldSymbol field)
        : base(field)
    {
    }
}

internal sealed class PropertySymbol : SymbolBase<Internal.Symbols.PropertySymbol>, IPropertySymbol
{
    public bool IsMutable => this.Symbol.IsMutable;
    public bool IsStatic => this.Symbol.IsStatic;
    public ITypeSymbol Type => this.Symbol.Type.ToApiSymbol();

    public IFunctionSymbol? Getter => this.Symbol.Getter?.ToApiSymbol();

    public IFunctionSymbol? Setter => this.Symbol.Setter?.ToApiSymbol();

    public PropertySymbol(Internal.Symbols.PropertySymbol property)
        : base(property)
    {
    }
}

internal sealed class GlobalSymbol : SymbolBase<Internal.Symbols.GlobalSymbol>, IGlobalSymbol
{
    public bool IsMutable => this.Symbol.IsMutable;
    public bool IsStatic => this.Symbol.IsStatic;
    public ITypeSymbol Type => this.Symbol.Type.ToApiSymbol();

    public GlobalSymbol(Internal.Symbols.GlobalSymbol global)
        : base(global)
    {
    }
}

internal sealed class LocalSymbol : SymbolBase<Internal.Symbols.LocalSymbol>, ILocalSymbol
{
    public bool IsMutable => this.Symbol.IsMutable;
    public ITypeSymbol Type => this.Symbol.Type.ToApiSymbol();

    public LocalSymbol(Internal.Symbols.LocalSymbol local)
        : base(local)
    {
    }
}

internal sealed class ParameterSymbol : SymbolBase<Internal.Symbols.ParameterSymbol>, IParameterSymbol
{
    public bool IsMutable => this.Symbol.IsMutable;
    public ITypeSymbol Type => this.Symbol.Type.ToApiSymbol();

    public ParameterSymbol(Internal.Symbols.ParameterSymbol parameter)
        : base(parameter)
    {
    }
}

internal sealed class FunctionSymbol : SymbolBase<Internal.Symbols.FunctionSymbol>, IFunctionSymbol
{
    public ITypeSymbol Type => this.Symbol.Type.ToApiSymbol();
    public ITypeSymbol ReturnType => this.Symbol.ReturnType.ToApiSymbol();
    public bool IsStatic => this.Symbol.IsStatic;

    public ImmutableArray<IParameterSymbol> Parameters => this.Symbol.Parameters
        .Select(s => s.ToApiSymbol())
        .ToImmutableArray();

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
    public bool IsStatic => this.Symbol.IsStatic;
    public TypeSymbol(Internal.Symbols.TypeSymbol type)
        : base(type)
    {
    }
}

internal sealed class TypeParameterSymbol : SymbolBase<Internal.Symbols.TypeParameterSymbol>, ITypeParameterSymbol
{
    public bool IsStatic => this.Symbol.IsStatic;
    public TypeParameterSymbol(Internal.Symbols.TypeParameterSymbol type)
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
