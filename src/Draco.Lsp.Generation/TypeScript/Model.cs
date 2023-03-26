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
/// <param name="GenericParams">The generic parameters of the declared interface.</param>
/// <param name="Bases">The base types of the interface.</param>
/// <param name="Fields">The fields this interface defines.</param>
internal sealed record class Interface(
    string? Documentation,
    string Name,
    ImmutableArray<string> GenericParams,
    ImmutableArray<Expression> Bases,
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
    Expression Type) : Declaration(Documentation);

/// <summary>
/// A namespace declaration.
/// </summary>
/// <param name="Documentation">The optional documentation for this declaration.</param>
/// <param name="Name">The name of the namespace.</param>
/// <param name="Constants">The constants defined within this namespace.</param>
internal sealed record class Namespace(
    string? Documentation,
    string Name,
    ImmutableArray<Constant> Constants) : Declaration(Documentation);

/// <summary>
/// An enum declaration.
/// </summary>
/// <param name="Documentation">The optional documentation for this declaration.</param>
/// <param name="Name">The name of the enum.</param>
/// <param name="Members">The enum member names with their values assigned.</param>
internal sealed record class Enum(
    string? Documentation,
    string Name,
    ImmutableArray<KeyValuePair<string, Expression>> Members) : Declaration(Documentation);

/// <summary>
/// A constant declaration.
/// </summary>
/// <param name="Documentation">The optional documentation for this declaration.</param>
/// <param name="Name">The name of the constant.</param>
/// <param name="Value">The value of the constant.</param>
internal sealed record class Constant(
    string? Documentation,
    string Name,
    Expression Value) : Declaration(Documentation);

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
    Expression Type) : Field(Documentation);

/// <summary>
/// An index signature, like '[key: Foo]: Bar'.
/// </summary>
/// <param name="KeyName">The dictionary key name.</param>
/// <param name="KeyType">The dictionary key type.</param>
/// <param name="ValueType">The dictionary value type.</param>
internal sealed record class IndexSignature(
    string KeyName,
    Expression KeyType,
    Expression ValueType) : Field(null as string);

/// <summary>
/// The base of any expression.
/// </summary>
internal abstract record class Expression;

/// <summary>
/// An integer constant.
/// </summary>
/// <param name="Value">The integral value.</param>
internal sealed record class IntExpression(int Value) : Expression;

/// <summary>
/// A string constant.
/// </summary>
/// <param name="Value">The string value.</param>
internal sealed record class StringExpression(string Value) : Expression;

/// <summary>
/// A name reference.
/// </summary>
/// <param name="Name">The name.</param>
internal sealed record class NameExpression(string Name) : Expression;

/// <summary>
/// A negation.
/// </summary>
/// <param name="Operand">The negated expression.</param>
internal sealed record class NegateExpression(Expression Operand) : Expression;

/// <summary>
/// An array literal.
/// </summary>
/// <param name="Elements">The array of values.</param>
internal sealed record class ArrayExpression(ImmutableArray<Expression> Elements) : Expression;

/// <summary>
/// A member access.
/// </summary>
/// <param name="Object">The object that has the accessed member.</param>
/// <param name="Member">The access member name.</param>
internal sealed record class MemberExpression(Expression Object, string Member) : Expression;

/// <summary>
/// An union type reference, like 'Alt1 | Alt2 | ...'.
/// </summary>
/// <param name="Alternatives">The alternative types.</param>
internal sealed record class UnionTypeExpression(
    ImmutableArray<Expression> Alternatives) : Expression;

/// <summary>
/// An inline, anonymous type, like '{ field1: Type1; field2: Type2; ... }'.
/// </summary>
/// <param name="Fields">The fields within the anonymous type.</param>
internal sealed record class AnonymousTypeExpression(
    ImmutableArray<Field> Fields) : Expression;

/// <summary>
/// An array type reference, like 'ElementType[]'.
/// </summary>
/// <param name="ElementType">The array element type.</param>
internal sealed record class ArrayTypeExpression(
    Expression ElementType) : Expression;
