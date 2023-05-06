using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.UntypedTree;

/// <summary>
/// The base for all untyped nodes in the untyped syntax tree.
/// </summary>
internal abstract partial class UntypedNode
{
    public SyntaxNode? Syntax { get; }

    protected UntypedNode(SyntaxNode? syntax)
    {
        this.Syntax = syntax;
    }
}

// Statements

internal partial class UntypedNoOpStatement
{
    public static UntypedNoOpStatement Default { get; } = new(null);
}

// Expressions

internal partial class UntypedExpression
{
    public virtual TypeSymbol? Type => null;

    public TypeSymbol TypeRequired => this.Type ?? IntrinsicSymbols.ErrorType;
}

internal partial class UntypedUnexpectedExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.ErrorType;
}

internal partial class UntypedUnitExpression
{
    public static UntypedUnitExpression Default { get; } = new(null);
    public override TypeSymbol Type => IntrinsicSymbols.Unit;
}

internal partial class UntypedMemberExpression
{
    public override TypeSymbol? Type => this.MemberType;
}

internal partial class UntypedGotoExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.Never;
}

internal partial class UntypedReturnExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.Never;
}

internal partial class UntypedBlockExpression
{
    public override TypeSymbol Type => this.Value.TypeRequired;
}

internal partial class UntypedWhileExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.Unit;
}

internal partial class UntypedAndExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.Bool;
}

internal partial class UntypedOrExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.Bool;
}

internal partial class UntypedParameterExpression
{
    public override TypeSymbol Type => this.Parameter.Type;
}

internal partial class UntypedGlobalExpression
{
    public override TypeSymbol Type => this.Global.Type;
}

internal partial class UntypedFieldExpression
{
    public override TypeSymbol Type => this.Field.Type;
}

internal partial class UntypedReferenceErrorExpression
{
    public override TypeSymbol? Type => IntrinsicSymbols.ErrorType;
}

internal partial class UntypedLiteralExpression
{
    public override TypeSymbol Type => this.Value switch
    {
        int => IntrinsicSymbols.Int32,
        bool => IntrinsicSymbols.Bool,
        double => IntrinsicSymbols.Float64,
        _ => throw new System.InvalidOperationException(),
    };
}

internal partial class UntypedStringExpression
{
    public override TypeSymbol? Type => IntrinsicSymbols.String;
}

internal partial class UntypedRelationalExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.Bool;
}

internal partial class UntypedAssignmentExpression
{
    public override TypeSymbol Type => this.Left.Type;
}

// Lvalues

internal partial class UntypedUnexpectedLvalue
{
    public override TypeSymbol Type => IntrinsicSymbols.ErrorType;
}

internal partial class UntypedIllegalLvalue
{
    public override TypeSymbol Type => IntrinsicSymbols.ErrorType;
}

internal partial class UntypedLvalue
{
    public abstract TypeSymbol Type { get; }
}

internal partial class UntypedGlobalLvalue
{
    public override TypeSymbol Type => this.Global.Type;
}
