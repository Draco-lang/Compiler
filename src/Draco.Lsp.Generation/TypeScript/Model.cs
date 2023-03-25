using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Lsp.Generation.TypeScript;

/// <summary>
/// Represents all TypeScript elements parsed up.
/// </summary>
/// <param name="Declaration">The declarations within this model.</param>
internal sealed record class Model(ImmutableArray<Declaration> Declaration);

/// <summary>
/// The base of all declarations.
/// </summary>
/// <param name="Documentation">The optional documentation for this declaration.</param>
internal abstract record class Declaration(string? Documentation);

/// <summary>
/// An interface declaration.
/// </summary>
/// <param name="Documentation">The optional documentation for this declaration.</param>
/// <param name="Name">The name of the declared interface.</param>
/// <param name="Fields">The fields this interface defines.</param>
internal sealed record class Interface(
    string? Documentation,
    string Name,
    ImmutableArray<Field> Fields) : Declaration(Documentation);

/// <summary>
/// A type-alias declaration.
/// </summary>
/// <param name="Documentation">The optional documentation for this declaration.</param>
/// <param name="Name">The name of the type alias.</param>
/// <param name="Type">The aliased type.</param>
internal sealed record class TypeAlias(
    string? Documentation,
    string Name,
    Type Type) : Declaration(Documentation);

/// <summary>
/// The base of a TypeScript type reference.
/// </summary>
internal abstract record class Type;

/// <summary>
/// A simple named type reference, like 'Foo'.
/// </summary>
/// <param name="Name">The type name.</param>
internal sealed record class NameType(
    string Name) : Type;

/// <summary>
/// An array type reference, like 'ElementType[]'.
/// </summary>
/// <param name="ElementType">The array element type.</param>
internal sealed record class ArrayType(
    Type ElementType) : Type;

/// <summary>
/// An union type reference, like 'Alt1 | Alt2 | ...'.
/// </summary>
/// <param name="Alternatives">The alternative types.</param>
internal sealed record class UnionType(
    ImmutableArray<Type> Alternatives) : Type;

/// <summary>
/// An inline, anonymous type, like '{ field1: Type1; field2: Type2; ... }'.
/// </summary>
/// <param name="Fields">The fields within the anonymous type.</param>
internal sealed record class AnonymousType(
    ImmutableArray<Field> Fields) : Type;

/// <summary>
/// Any kind of field.
/// </summary>
/// <param name="Documentation">The optional field documentation.</param>
internal abstract record class Field(
    string? Documentation);

/// <summary>
/// A field declaration.
/// </summary>
/// <param name="Documentation">The optional field documentation.</param>
/// <param name="Name">The name of the field.</param>
/// <param name="Nullable">True, if this is a nullable field.</param>
/// <param name="Type">The type of the field.</param>
internal sealed record class SimpleField(
    string? Documentation,
    string Name,
    bool Nullable,
    Type Type) : Field(Documentation);

/// <summary>
/// An index signature, like '[key: Foo]: Bar'.
/// </summary>
/// <param name="KeyName">The dictionary key name.</param>
/// <param name="KeyType">The dictionary key type.</param>
/// <param name="ValueType">The dictionary value type.</param>
internal sealed record class IndexSignature(
    string KeyName,
    Type KeyType,
    Type ValueType) : Field(null as string);
