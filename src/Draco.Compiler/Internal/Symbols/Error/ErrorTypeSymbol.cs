using System.Collections.Immutable;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents a type of some type-checking error. Acts as a sentinel value, absorbs cascading errors.
/// </summary>
internal sealed class ErrorTypeSymbol(string name) : TypeSymbol
{
    public override bool IsError => true;
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;

    /// <summary>
    /// The display name of the type.
    /// </summary>
    public string DisplayName { get; } = name;

    public override string ToString() => this.DisplayName;

    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) => this;
    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) => this;
}
