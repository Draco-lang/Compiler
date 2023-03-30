using System;

namespace Draco.Compiler.Internal.Types;

/// <summary>
/// The base for all types within the language.
/// </summary>
internal abstract partial class Type
{
    /// <summary>
    /// True, if this is a type variable, false otherwise.
    /// </summary>
    public virtual bool IsTypeVariable => false;

    /// <summary>
    /// True, if this is type represents some error.
    /// </summary>
    public virtual bool IsError => false;

    public override bool Equals(object? obj) => throw new InvalidOperationException("do not use equality for types");
    public override int GetHashCode() => throw new InvalidOperationException("do not use equality for types");

    public abstract override string ToString();

    /// <summary>
    /// Converts this type into an API type.
    /// </summary>
    /// <returns>The equivalent API type.</returns>
    public Api.Semantics.IType ToApiType() => new Api.Semantics.Type(this);
}
