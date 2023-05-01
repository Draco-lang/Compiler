using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a free-function.
/// </summary>
internal abstract partial class FunctionSymbol : Symbol, ITypedSymbol
{
    /// <summary>
    /// Retrieves the name for the unary operator that is referenced by a given token.
    /// </summary>
    /// <param name="token">The <see cref="TokenKind"/> that references the unary operator.</param>
    /// <returns>The name of the symbol to look up the unary operator.</returns>
    public static string GetUnaryOperatorName(TokenKind token) => token switch
    {
        TokenKind.Plus => "operator +",
        TokenKind.Minus => "operator -",
        TokenKind.KeywordNot => "operator not",
        _ => throw new System.ArgumentOutOfRangeException(nameof(token)),
    };

    /// <summary>
    /// Retrieves the name for the binary operator that is referenced by a given token.
    /// </summary>
    /// <param name="token">The <see cref="TokenKind"/> that references the binary operator.</param>
    /// <returns>The name of the symbol to look up the binary operator.</returns>
    public static string GetBinaryOperatorName(TokenKind token) => token switch
    {
        TokenKind.Plus => "operator +",
        TokenKind.Minus => "operator -",
        TokenKind.Star => "operator *",
        TokenKind.Slash => "operator /",
        TokenKind.KeywordMod => "operator mod",
        TokenKind.KeywordRem => "operator rem",
        _ => throw new System.ArgumentOutOfRangeException(nameof(token)),
    };

    /// <summary>
    /// Retrieves the name for the comparison operator that is referenced by a given token.
    /// </summary>
    /// <param name="token">The <see cref="TokenKind"/> that references the comparison operator.</param>
    /// <returns>The name of the symbol to look up the comparison operator.</returns>
    public static string GetComparisonOperatorName(TokenKind token) => token switch
    {
        TokenKind.Equal => "operator ==",
        TokenKind.NotEqual => "operator !=",
        TokenKind.GreaterThan => "operator >",
        TokenKind.LessThan => "operator <",
        TokenKind.GreaterEqual => "operator >=",
        TokenKind.LessEqual => "operator <=",
        _ => throw new System.ArgumentOutOfRangeException(nameof(token)),
    };

    /// <summary>
    /// The parameters of this function.
    /// </summary>
    public abstract ImmutableArray<ParameterSymbol> Parameters { get; }

    /// <summary>
    /// The return type of this function.
    /// </summary>
    public abstract TypeSymbol ReturnType { get; }

    /// <summary>
    /// If true, this is a member function.
    /// </summary>
    public virtual bool IsMember => false;

    /// <summary>
    /// If true, this is a virtual function.
    /// </summary>
    public virtual bool IsVirtual => false;

    public override IEnumerable<Symbol> Members => this.Parameters;

    public TypeSymbol Type => this.type ??= this.BuildType();
    private TypeSymbol? type;

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append(this.Name);
        if (this.GenericParameters.Length > 0)
        {
            result.Append('<');
            result.AppendJoin(", ", this.GenericParameters);
            result.Append('>');
        }
        result.Append('(');
        result.AppendJoin(", ", this.Parameters);
        result.Append($"): {this.ReturnType}");
        return result.ToString();
    }

    public override FunctionSymbol GenericInstantiate(GenericContext context) =>
        new FunctionInstanceSymbol(this, context);

    public override Api.Semantics.ISymbol ToApiSymbol() => new Api.Semantics.FunctionSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitFunction(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitFunction(this);

    private TypeSymbol BuildType() => new FunctionTypeSymbol(this.Parameters, this.ReturnType);
}
