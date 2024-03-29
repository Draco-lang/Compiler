using System;
using System.Collections.Immutable;
using System.Linq;

namespace Draco.SourceGeneration.Dap.CsModel;

/// <summary>
/// A C# type.
/// </summary>
public abstract record class Type
{
    /// <summary>
    /// A discriminator string for Scriban.
    /// </summary>
    public string Discriminator
    {
        get
        {
            var name = this.GetType().Name;
            if (name.EndsWith("Type")) name = name[..^4];
            return name;
        }
    }
}

/// <summary>
/// A type backed by a C# declaration.
/// </summary>
/// <param name="Declaration">The referenced C# declaration.</param>
public sealed record class DeclarationType(Declaration Declaration) : Type;

/// <summary>
/// A builtin C# type.
/// </summary>
/// <param name="FullName">The full name of the reflected type.</param>
public sealed record class BuiltinType(string FullName) : Type;

/// <summary>
/// A nullable C# type.
/// </summary>
/// <param name="Type">The underlying type.</param>
public sealed record class NullableType(Type Type) : Type;

/// <summary>
/// An array type.
/// </summary>
/// <param name="ElementType">The array element type.</param>
public sealed record class ArrayType(Type ElementType) : Type;

/// <summary>
/// A type representing DUs.
/// </summary>
/// <param name="Alternatives">The alternative types.</param>
public sealed record class DiscriminatedUnionType(ImmutableArray<Type> Alternatives) : Type
{
    public bool Equals(DiscriminatedUnionType other) =>
        this.Alternatives.SequenceEqual(other.Alternatives);

    public override int GetHashCode()
    {
        var h = default(HashCode);
        foreach (var a in this.Alternatives) h.Add(a);
        return h.ToHashCode();
    }
}

/// <summary>
/// A C# dictionary type.
/// </summary>
/// <param name="KeyType">The key type.</param>
/// <param name="ValueType">The value type.</param>
public sealed record class DictionaryType(Type KeyType, Type ValueType) : Type;
