using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A function synthetized by the compiler.
/// </summary>
internal sealed class SynthetizedFunctionSymbol : FunctionSymbol
{
    /// <summary>
    /// Constructs a function symbol for an unary operator.
    /// </summary>
    /// <param name="token">The <see cref="TokenKind"/> for the unary operator.</param>
    /// <param name="operandType">The operand type.</param>
    /// <param name="returnType">The return type.</param>
    /// <returns>The constructed function symbol.</returns>
    public static FunctionSymbol UnaryOperator(TokenKind token, Type operandType, Type returnType) =>
        new SynthetizedFunctionSymbol(GetUnaryOperatorName(token), new[] { operandType }, returnType);

    /// <summary>
    /// Constructs a function symbol for a binary operator.
    /// </summary>
    /// <param name="token">The <see cref="TokenKind"/> for the binary operator.</param>
    /// <param name="leftType">The left operand type.</param>
    /// <param name="rightType">The right operand type.</param>
    /// <param name="returnType">The return type.</param>
    /// <returns>The constructed function symbol.</returns>
    public static FunctionSymbol BinaryOperator(TokenKind token, Type leftType, Type rightType, Type returnType) =>
        new SynthetizedFunctionSymbol(GetBinaryOperatorName(token), new[] { leftType, rightType }, returnType);

    /// <summary>
    /// Constructs a function symbol for a comparison operator.
    /// </summary>
    /// <param name="token">The <see cref="TokenKind"/> for the comparison operator.</param>
    /// <param name="leftType">The left operand type.</param>
    /// <param name="rightType">The right operand type.</param>
    /// <returns>The constructed function symbol.</returns>
    public static FunctionSymbol ComparisonOperator(TokenKind token, Type leftType, Type rightType) =>
        new SynthetizedFunctionSymbol(GetComparisonOperatorName(token), new[] { leftType, rightType }, Types.Intrinsics.Bool);

    public override ImmutableArray<ParameterSymbol> Parameters { get; }

    public override Type ReturnType { get; }
    public override Symbol? ContainingSymbol => throw new System.NotImplementedException();

    public override string Name { get; }

    public SynthetizedFunctionSymbol(string name, IEnumerable<Type> paramTypes, Type returnType)
    {
        this.Name = name;
        this.Parameters = paramTypes
            .Select(t => new SynthetizedParameterSymbol(t))
            .Cast<ParameterSymbol>()
            .ToImmutableArray();
        this.ReturnType = returnType;
    }

    public override ISymbol ToApiSymbol() => throw new System.NotImplementedException();
}
