using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Draco.SourceGeneration.Lsp.CSharp;

// NOTE: This model is mutable on purpose, it's easier to build it

/// <summary>
/// The C# model of the LSP code.
/// </summary>
public sealed class Model
{
    /// <summary>
    /// The declarations of the model.
    /// </summary>
    public IList<Declaration> Declarations { get; set; } = new List<Declaration>();
}

/// <summary>
/// The base of all declarations.
/// </summary>
public abstract class Declaration
{
    /// <summary>
    /// A discriminator string for Scriban.
    /// </summary>
    public string Discriminator => this.GetType().Name;

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
public sealed class Class : Declaration
{
    /// <summary>
    /// The parent of this class in terms of containment, not inheritance.
    /// </summary>
    public Class? Parent { get; set; }

    /// <summary>
    /// The interfaces this class implements.
    /// </summary>
    public IList<Interface> Interfaces { get; set; } = new List<Interface>();

    /// <summary>
    /// The declarations this class has nested within it.
    /// </summary>
    public IList<Declaration> NestedDeclarations { get; set; } = new List<Declaration>();

    /// <summary>
    /// The properties within this class.
    /// </summary>
    public IList<Property> Properties { get; set; } = new List<Property>();

    /// <summary>
    /// Initializes the parent-child relationship between this class and the nested classes.
    /// </summary>
    public void InitializeParents()
    {
        foreach (var subclass in this.NestedDeclarations.OfType<Class>())
        {
            subclass.Parent = this;
            subclass.InitializeParents();
        }
    }
}

/// <summary>
/// An interface declaration.
/// </summary>
public sealed class Interface : Declaration
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
public sealed class Enum : Declaration
{
    /// <summary>
    /// True, if this is a string enum.
    /// </summary>
    public bool IsStringEnum => this.Members.Any(m => m.SerializedValue is string);

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
/// <param name="SerializedValue">The serialized enum member value.</param>
public sealed record class EnumMember(
    string? Documentation,
    string Name,
    object? SerializedValue)
{
    /// <summary>
    /// A discriminator string for the value.
    /// </summary>
    public string ValueDiscriminator => this.SerializedValue switch
    {
        int => "Int",
        string => "String",
        _ => throw new NotImplementedException(),
    };
}

/// <summary>
/// A property definition.
/// </summary>
/// <param name="Documentation">The docs of the property.</param>
/// <param name="Type">The property type.</param>
/// <param name="Name">The property name.</param>
/// <param name="SerializedName">The property name when serialized.</param>
/// <param name="OmitIfNull">True, if the property shold be omitted, if it's null.</param>
/// <param name="IsExtensionData">True, if the property represents extension data.</param>
public sealed record class Property(
    string? Documentation,
    Type Type,
    string Name,
    string SerializedName,
    bool OmitIfNull,
    bool IsExtensionData);

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
            if (name.EndsWith("Type")) name = name.Substring(0, name.Length - 4);
            return name;
        }
    }
}

/// <summary>
/// A type backed by a C# declaration.
/// </summary>
/// <param name="Declaration">The referenced C# declaration.</param>
public sealed record class DeclarationType(
    Declaration Declaration) : Type;

/// <summary>
/// A builtin C# type.
/// </summary>
/// <param name="FullName">The full name of the reflected type.</param>
public sealed record class BuiltinType(
    string FullName) : Type;

/// <summary>
/// A type representing DUs.
/// </summary>
/// <param name="Alternatives">The alternative types.</param>
public sealed record class DiscriminatedUnionType(
    ImmutableArray<Type> Alternatives) : Type;

/// <summary>
/// An array type.
/// </summary>
/// <param name="ElementType">The array element type.</param>
public sealed record class ArrayType(
    Type ElementType) : Type;

/// <summary>
/// A nullable C# type.
/// </summary>
/// <param name="Type">The underlying type.</param>
public sealed record class NullableType(
    Type Type) : Type;

/// <summary>
/// A C# dictionary type.
/// </summary>
/// <param name="KeyType">The key type.</param>
/// <param name="ValueType">The value type.</param>
public sealed record class DictionaryType(
    Type KeyType,
    Type ValueType) : Type;
