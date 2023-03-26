using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Lsp.Generation.CSharp;

// NOTE: This model is mutable on purpose, it's easier to build it

/// <summary>
/// The C# model of the LSP code.
/// </summary>
internal sealed class Model
{
    /// <summary>
    /// The declarations of the model.
    /// </summary>
    public IList<Declaration> Declarations { get; set; } = new List<Declaration>();
}

/// <summary>
/// The base of all declarations.
/// </summary>
internal abstract class Declaration
{
    /// <summary>
    /// The docs of this declaration.
    /// </summary>
    public string? Documentation { get; set; } = null;

    /// <summary>
    /// The name of this declaration.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// A class declaration.
/// </summary>
internal sealed class Class : Declaration
{
    /// <summary>
    /// The interfaces this class implements.
    /// </summary>
    public IList<Interface> Interfaces { get; set; } = new List<Interface>();

    /// <summary>
    /// The properties within this class.
    /// </summary>
    public IList<Property> Properties { get; set; } = new List<Property>();
}

/// <summary>
/// An interface declaration.
/// </summary>
internal sealed class Interface : Declaration
{
    /// <summary>
    /// The interfaces this interface implements.
    /// </summary>
    public IList<Interface> Interfaces { get; set; } = new List<Interface>();

    /// <summary>
    /// The properties within this interface.
    /// </summary>
    public IList<Property> Properties { get; set; } = new List<Property>();
}

/// <summary>
/// An enum declaration.
/// </summary>
internal sealed class Enum : Declaration
{
    /// <summary>
    /// The members within this enum.
    /// </summary>
    public IList<EnumMember> Members { get; set; } = new List<EnumMember>();
}

/// <summary>
/// A single enum member.
/// </summary>
/// <param name="Documentation">The documentation of this member.</param>
/// <param name="Name">The name of the enum member.</param>
/// <param name="Attributes">The attributes for this property.</param>
internal sealed record class EnumMember(
    string? Documentation,
    string Name,
    ImmutableArray<Attribute> Attributes);

/// <summary>
/// A property definition.
/// </summary>
/// <param name="Documentation">The docs of the property.</param>
/// <param name="Type">The property type.</param>
/// <param name="Nullable">True, if this field is nullable.</param>
/// <param name="Name">The property name.</param>
/// <param name="Attributes">The attributes for this property.</param>
internal sealed record class Property(
    string? Documentation,
    Type Type,
    string Name,
    ImmutableArray<Attribute> Attributes);

/// <summary>
/// An attribute for some element.
/// </summary>
/// <param name="Name">The attribute name.</param>
/// <param name="Args">The attribute arguments.</param>
internal sealed record class Attribute(
    string Name,
    ImmutableArray<object?> Args);

/// <summary>
/// A C# type.
/// </summary>
internal abstract record class Type;

/// <summary>
/// A type backed by a C# declaration.
/// </summary>
/// <param name="Declaration">The referenced C# declaration.</param>
internal sealed record class DeclarationType(
    Declaration Declaration) : Type;

/// <summary>
/// A builtin C# type.
/// </summary>
/// <param name="Type">The reflected type.</param>
internal sealed record class BuiltinType(
    System.Type Type) : Type;

/// <summary>
/// A type representing DUs.
/// </summary>
/// <param name="Alternatives">The alternative types.</param>
internal sealed record class DiscriminatedUnionType(
    ImmutableArray<Type> Alternatives) : Type;

/// <summary>
/// An array type.
/// </summary>
/// <param name="ElementType">The array element type.</param>
internal sealed record class ArrayType(
    Type ElementType) : Type;

/// <summary>
/// A nullable C# type.
/// </summary>
/// <param name="Type">The underlying type.</param>
internal sealed record class NullableType(
    Type Type) : Type;

/// <summary>
/// A C# dictionary type.
/// </summary>
/// <param name="KeyType">The key type.</param>
/// <param name="ValueType">The value type.</param>
internal sealed record class DictionaryType(
    Type KeyType,
    Type ValueType) : Type;

/// <summary>
/// Utilities for constructing attributes for the model.
/// </summary>
internal static class Attributes
{
    public static Attribute Optional() => new("Optional", ImmutableArray<object?>.Empty);
    public static Attribute JsonProperty(string name) => new("JsonProperty", ImmutableArray.Create<object?>(name));
    public static Attribute JsonValue(object? value) => new("JsonValue", ImmutableArray.Create<object?>(value));
}
