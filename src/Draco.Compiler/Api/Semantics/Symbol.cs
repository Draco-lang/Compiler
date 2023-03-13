using System;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;

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
    /// The list of <see cref="Diagnostic"/> messages attached to this symbol.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

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
/// Represents a label symbol.
/// </summary>
public interface ILabelSymbol : ISymbol
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
}

// TODO
/*
// Proxy classes ///////////////////////////////////////////////////////////////

internal abstract class SymbolBase : ISymbol
{
    public IInternalSymbol Symbol { get; }

    public string Name => this.Symbol.Name;
    public bool IsError => this.Symbol.IsError;
    public ImmutableArray<Diagnostic> Diagnostics => this.Symbol.Diagnostics
        .Select(d => d.ToApiDiagnostic(null))
        .ToImmutableArray();

    public Location? Definition => this.Symbol.Definition?.Location;
    public string Documentation => this.Symbol.Documentation;

    public SymbolBase(IInternalSymbol symbol)
    {
        this.Symbol = symbol;
    }

    public bool Equals(ISymbol? other) => other is SymbolBase o
                                       && ReferenceEquals(this.Symbol, o.Symbol);

    public override int GetHashCode() => this.Symbol.GetHashCode();
}

internal abstract class SymbolBase<TInternalSymbol> : SymbolBase
    where TInternalSymbol : IInternalSymbol
{
    public new TInternalSymbol Symbol => (TInternalSymbol)base.Symbol;

    protected SymbolBase(TInternalSymbol symbol)
        : base(symbol)
    {
    }
}

internal sealed class ErrorSymbol : SymbolBase<IInternalSymbol>
{
    public ErrorSymbol(IInternalSymbol symbol)
        : base(symbol)
    {
    }
}

internal sealed class VariableSymbol : SymbolBase<IInternalSymbol.IVariable>, IVariableSymbol
{
    public bool IsMutable => this.Symbol.IsMutable;

    public VariableSymbol(IInternalSymbol.IVariable variable)
        : base(variable)
    {
    }
}

internal sealed class LabelSymbol : SymbolBase<IInternalSymbol.ILabel>, ILabelSymbol
{
    public LabelSymbol(IInternalSymbol.ILabel label)
        : base(label)
    {
    }
}

internal sealed class ParameterSymbol : SymbolBase<IInternalSymbol.IParameter>, IParameterSymbol
{
    public bool IsMutable => this.Symbol.IsMutable;

    public ParameterSymbol(IInternalSymbol.IParameter parameter)
        : base(parameter)
    {
    }
}

internal sealed class FunctionSymbol : SymbolBase<IInternalSymbol.IFunction>, IFunctionSymbol
{
    public FunctionSymbol(IInternalSymbol.IFunction function)
        : base(function)
    {
    }
}

internal sealed class TypeSymbol : SymbolBase<IInternalSymbol.ITypeDefinition>, ITypeSymbol
{
    public TypeSymbol(IInternalSymbol.ITypeDefinition typeDef)
        : base(typeDef)
    {
    }
}
*/
