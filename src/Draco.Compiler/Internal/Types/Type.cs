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

    public override abstract string ToString();

    /// <summary>
    /// Checks if the type contains certain <see cref="TypeVariable"/>.
    /// </summary>
    /// <param name="variable">The <see cref="TypeVariable"/> the type could contain.</param>
    /// <returns>True, if this type contains <paramref name="variable"/>.</returns>
    public virtual bool ContainsTypeVariable(TypeVariable variable) => false;

    /// <summary>
    /// Converts this type into an API type.
    /// </summary>
    /// <returns>The equivalent API type.</returns>
    public Api.Semantics.IType ToApiType() => new Api.Semantics.Type(this);
}
