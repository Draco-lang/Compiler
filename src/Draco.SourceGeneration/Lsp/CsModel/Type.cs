using System.Collections.Immutable;

namespace Draco.SourceGeneration.Lsp.CsModel;

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
/// A type representing DUs.
/// </summary>
/// <param name="Alternatives">The alternative types.</param>
public sealed record class DiscriminatedUnionType(ImmutableArray<Type> Alternatives) : Type;

/// <summary>
/// An array type.
/// </summary>
/// <param name="ElementType">The array element type.</param>
public sealed record class ArrayType(Type ElementType) : Type;

/// <summary>
/// A nullable C# type.
/// </summary>
/// <param name="Type">The underlying type.</param>
public sealed record class NullableType(Type Type) : Type;

/// <summary>
/// A C# dictionary type.
/// </summary>
/// <param name="KeyType">The key type.</param>
/// <param name="ValueType">The value type.</param>
public sealed record class DictionaryType(Type KeyType, Type ValueType) : Type;

/// <summary>
/// A C# tuple type.
/// </summary>
/// <param name="Elements">The types of the tuple elements.</param>
public sealed record class TupleType(ImmutableArray<Type> Elements) : Type;
