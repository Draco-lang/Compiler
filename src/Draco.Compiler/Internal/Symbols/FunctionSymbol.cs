using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.OptimizingIr;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a free-function.
/// </summary>
internal abstract partial class FunctionSymbol : Symbol, ITypedSymbol, IMemberSymbol, IOverridableSymbol
{
    /// <summary>
    /// A delegate to generate IR code.
    /// </summary>
    /// <param name="codegen">The code generator.</param>
    /// <param name="target">The register to store the result at.</param>
    /// <param name="operands">The compiled operand references.</param>
    public delegate void CodegenDelegate(
        FunctionBodyCodegen codegen,
        Register target,
        ImmutableArray<IOperand> operands);

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
    /// The receiver of this function, if it has one.
    /// </summary>
    public TypeSymbol? Receiver => this.IsStatic
        ? null
        : this.ContainingSymbol as TypeSymbol;

    /// <summary>
    /// The parameters of this function.
    /// </summary>
    public abstract ImmutableArray<ParameterSymbol> Parameters { get; }

    /// <summary>
    /// The return type of this function.
    /// </summary>
    public abstract TypeSymbol ReturnType { get; }

    public virtual bool IsStatic => true;

    /// <summary>
    /// If true, this is a virtual function.
    /// </summary>
    public virtual bool IsVirtual => false;

    /// <summary>
    /// True, if this is a variadic function.
    /// </summary>
    public bool IsVariadic => this.Parameters.Length > 0 && this.Parameters[^1].IsVariadic;

    /// <summary>
    /// True, if this is a constructor.
    /// </summary>
    public virtual bool IsConstructor => false;

    public override bool IsSpecialName => this.IsConstructor;

    // NOTE: We override for covariant return type
    public override FunctionSymbol? GenericDefinition => null;

    public override Api.Semantics.Visibility Visibility => this.DeclaringSyntax switch
    {
        FunctionDeclarationSyntax funcDecl => GetVisibilityFromTokenKind(funcDecl.VisibilityModifier?.Kind),
        _ => Api.Semantics.Visibility.Internal,
    };

    public override IEnumerable<Symbol> Members => this.Parameters;

    public TypeSymbol Type => InterlockedUtils.InitializeNull(ref this.type, this.BuildType);
    private TypeSymbol? type;

    public virtual Symbol? Override => null;

    /// <summary>
    /// The bound body of this function, if it has one.
    /// This is the case for in-source and certain synthesized functions.
    /// </summary>
    public virtual BoundStatement? Body => null;

    /// <summary>
    /// The code generator for this function, if it has one.
    /// This is the case for certain synthesized functions.
    /// </summary>
    public virtual CodegenDelegate? Codegen => null;

    /// <summary>
    /// True, if this function must be inlined.
    /// </summary>
    public virtual bool ForceInline => false;

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append(this.Name);
        result.Append(this.GenericsToString());
        result.Append('(');
        result.AppendJoin(", ", this.Parameters);
        result.Append($"): {this.ReturnType}");
        return result.ToString();
    }

    public override bool CanBeShadowedBy(Symbol other)
    {
        if (other is not FunctionSymbol function) return false;
        if (this.Name != function.Name) return false;
        if (this.GenericParameters.Length != function.GenericParameters.Length) return false;
        if (this.Parameters.Length != function.Parameters.Length) return false;
        for (var i = 0; i < this.Parameters.Length; i++)
        {
            if (!SymbolEqualityComparer.AllowTypeVariables.Equals(this.Parameters[i].Type, function.Parameters[i].Type)) return false;
            if (this.Parameters[i].IsVariadic != function.Parameters[i].IsVariadic) return false;
        }
        return true;
    }

    public bool CanBeOverriddenBy(IOverridableSymbol other)
    {
        if (other is not FunctionSymbol function) return false;
        if (!this.CanBeShadowedBy(function)) return false;
        return SymbolEqualityComparer.AllowTypeVariables.IsBaseOf(this.ReturnType, function.ReturnType);
    }

    public override FunctionSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        (FunctionSymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override FunctionSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        new FunctionInstanceSymbol(containingSymbol, this, context);

    public override Api.Semantics.IFunctionSymbol ToApiSymbol() => new Api.Semantics.FunctionSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitFunction(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitFunction(this);

    private TypeSymbol BuildType() => new FunctionTypeSymbol(this.Parameters, this.ReturnType);
}
