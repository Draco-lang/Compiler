using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A function intrinsic known by the compiler. This function has no implementation, as it is known by the compiler.
/// </summary>
internal sealed class IntrinsicFunctionSymbol : FunctionSymbol
{
    /// <summary>
    /// Constructs a function symbol for an unary operator.
    /// </summary>
    /// <param name="token">The <see cref="TokenKind"/> for the unary operator.</param>
    /// <param name="operandType">The operand type.</param>
    /// <param name="returnType">The return type.</param>
    /// <returns>The constructed function symbol.</returns>
    public static FunctionSymbol UnaryOperator(TokenKind token, TypeSymbol operandType, TypeSymbol returnType) =>
        new IntrinsicFunctionSymbol(GetUnaryOperatorName(token), new[] { operandType }, returnType);

    /// <summary>
    /// Constructs a function symbol for a binary operator.
    /// </summary>
    /// <param name="token">The <see cref="TokenKind"/> for the binary operator.</param>
    /// <param name="leftType">The left operand type.</param>
    /// <param name="rightType">The right operand type.</param>
    /// <param name="returnType">The return type.</param>
    /// <returns>The constructed function symbol.</returns>
    public static FunctionSymbol BinaryOperator(TokenKind token, TypeSymbol leftType, TypeSymbol rightType, TypeSymbol returnType) =>
        new IntrinsicFunctionSymbol(GetBinaryOperatorName(token), new[] { leftType, rightType }, returnType);

    /// <summary>
    /// Constructs a function symbol for a comparison operator.
    /// </summary>
    /// <param name="token">The <see cref="TokenKind"/> for the comparison operator.</param>
    /// <param name="leftType">The left operand type.</param>
    /// <param name="rightType">The right operand type.</param>
    /// <returns>The constructed function symbol.</returns>
    public static FunctionSymbol ComparisonOperator(TokenKind token, TypeSymbol leftType, TypeSymbol rightType) =>
        new IntrinsicFunctionSymbol(GetComparisonOperatorName(token), new[] { leftType, rightType }, IntrinsicSymbols.Bool);

    public override ImmutableArray<ParameterSymbol> Parameters { get; }

    public override TypeSymbol ReturnType { get; }
    public override Symbol? ContainingSymbol => null;
    public override bool IsSpecialName => true;
    public override bool IsStatic => true;

    public override string Name { get; }

    public IntrinsicFunctionSymbol(string name, IEnumerable<TypeSymbol> paramTypes, TypeSymbol returnType)
    {
        this.Name = name;
        this.Parameters = paramTypes
            .Select(t => new SynthetizedParameterSymbol(t))
            .Cast<ParameterSymbol>()
            .ToImmutableArray();
        this.ReturnType = returnType;
    }
}
