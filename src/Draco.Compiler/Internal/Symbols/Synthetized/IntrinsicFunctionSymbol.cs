using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A function intrinsic known by the compiler. This function has no implementation, as it is known by the compiler.
/// </summary>
internal class IntrinsicFunctionSymbol : FunctionSymbol
{
    /// <summary>
    /// Constructs a function symbol for an unary operator.
    /// </summary>
    /// <param name="token">The <see cref="TokenKind"/> for the unary operator.</param>
    /// <param name="operandType">The operand type.</param>
    /// <param name="returnType">The return type.</param>
    /// <param name="codegen">The code generation function.</param>
    /// <returns>The constructed function symbol.</returns>
    public static FunctionSymbol UnaryOperator(
        TokenKind token,
        TypeSymbol operandType,
        TypeSymbol returnType,
        IrFunctionSymbol.CodegenDelegate codegen) =>
        new IrFunctionSymbol(GetUnaryOperatorName(token), new[] { operandType }, returnType, codegen);

    /// <summary>
    /// Constructs a function symbol for a binary operator.
    /// </summary>
    /// <param name="token">The <see cref="TokenKind"/> for the binary operator.</param>
    /// <param name="leftType">The left operand type.</param>
    /// <param name="rightType">The right operand type.</param>
    /// <param name="returnType">The return type.</param>
    /// <param name="codegen">The code generation function.</param>
    /// <returns>The constructed function symbol.</returns>
    public static FunctionSymbol BinaryOperator(
        TokenKind token,
        TypeSymbol leftType,
        TypeSymbol rightType,
        TypeSymbol returnType,
        IrFunctionSymbol.CodegenDelegate codegen) =>
        new IrFunctionSymbol(GetBinaryOperatorName(token), new[] { leftType, rightType }, returnType, codegen);

    /// <summary>
    /// Constructs a function symbol for a comparison operator.
    /// </summary>
    /// <param name="token">The <see cref="TokenKind"/> for the comparison operator.</param>
    /// <param name="leftType">The left operand type.</param>
    /// <param name="rightType">The right operand type.</param>
    /// <param name="returnType">The return type of the comparison.</param>
    /// <param name="codegen">The code generation function.</param>
    /// <returns>The constructed function symbol.</returns>
    public static FunctionSymbol ComparisonOperator(
        TokenKind token,
        TypeSymbol leftType,
        TypeSymbol rightType,
        TypeSymbol returnType,
        IrFunctionSymbol.CodegenDelegate codegen) =>
        new IrFunctionSymbol(GetComparisonOperatorName(token), new[] { leftType, rightType }, returnType, codegen);

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
            .Select(t => new SynthetizedParameterSymbol(this, t))
            .Cast<ParameterSymbol>()
            .ToImmutableArray();
        this.ReturnType = returnType;
    }
}
