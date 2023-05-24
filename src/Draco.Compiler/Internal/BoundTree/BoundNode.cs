using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.BoundTree;

/// <summary>
/// The base for all bound nodes in the bound tree.
/// </summary>
internal abstract partial class BoundNode
{
    public SyntaxNode? Syntax { get; }

    protected BoundNode(SyntaxNode? syntax)
    {
        this.Syntax = syntax;
    }

    public abstract void Accept(BoundTreeVisitor visitor);
    public abstract TResult Accept<TResult>(BoundTreeVisitor<TResult> visitor);

    protected static bool Equals<T>(ImmutableArray<T> left, ImmutableArray<T> right)
    {
        if (left.Length != right.Length) return false;
        return left.SequenceEqual(right);
    }
}

// Statements

internal partial class BoundNoOpStatement
{
    public static BoundNoOpStatement Default { get; } = new(null);
}

// Expressions

internal partial class BoundExpression
{
    public virtual TypeSymbol? Type => null;

    public TypeSymbol TypeRequired => this.Type ?? IntrinsicSymbols.ErrorType;
}

internal partial class BoundUnexpectedExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.ErrorType;
}

internal partial class BoundSequencePointExpression
{
    public override TypeSymbol? Type => this.Expression.Type;
}

internal partial class BoundUnitExpression
{
    public static BoundUnitExpression Default { get; } = new(null);
    public override TypeSymbol Type => IntrinsicSymbols.Unit;
}

internal partial class BoundGotoExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.Never;
}

internal partial class BoundReturnExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.Never;
}

internal partial class BoundBlockExpression
{
    public override TypeSymbol Type => this.Value.TypeRequired;
}

internal partial class BoundWhileExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.Unit;
}

internal partial class BoundAndExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.Bool;
}

internal partial class BoundOrExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.Bool;
}

internal partial class BoundParameterExpression
{
    public override TypeSymbol Type => this.Parameter.Type;
}

internal partial class BoundGlobalExpression
{
    public override TypeSymbol Type => this.Global.Type;
}

internal partial class BoundFieldExpression
{
    public override TypeSymbol Type => this.Field.Type;
}

internal partial class BoundPropertyGetExpression
{
    public override TypeSymbol Type => this.Getter.ReturnType;
}

internal partial class BoundLocalExpression
{
    public override TypeSymbol Type => this.Local.Type;
}

internal partial class BoundReferenceErrorExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.ErrorType;
}

internal partial class BoundLiteralExpression
{
    public override TypeSymbol Type => this.Value switch
    {
        int => IntrinsicSymbols.Int32,
        bool => IntrinsicSymbols.Bool,
        string => IntrinsicSymbols.String,
        _ => throw new System.InvalidOperationException(),
    };
}

internal partial class BoundStringExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.String;
}

internal partial class BoundRelationalExpression
{
    public override TypeSymbol Type => IntrinsicSymbols.Bool;
}

internal partial class BoundAssignmentExpression
{
    public override TypeSymbol Type => this.Left.Type;
}

internal partial class BoundObjectCreationExpression
{
    public override TypeSymbol Type => this.ObjectType;
}

internal partial class BoundArrayCreationExpression
{
    public override TypeSymbol Type => new ArrayTypeSymbol(this.ElementType, this.Sizes.Length);
}

internal partial class BoundCallExpression
{
    public override TypeSymbol Type => this.Method.ReturnType;
}

// Lvalues

internal partial class BoundLvalue
{
    public abstract TypeSymbol Type { get; }
}

internal partial class BoundUnexpectedLvalue
{
    public override TypeSymbol Type => IntrinsicSymbols.ErrorType;
}

internal partial class BoundIllegalLvalue
{
    public override TypeSymbol Type => IntrinsicSymbols.ErrorType;
}

internal partial class BoundLocalLvalue
{
    public override TypeSymbol Type => this.Local.Type;
}

internal partial class BoundGlobalLvalue
{
    public override TypeSymbol Type => this.Global.Type;
}

internal partial class BoundFieldLvalue
{
    public override TypeSymbol Type => this.Field.Type;
}

internal partial class BoundPropertySetExpression
{
    public override TypeSymbol Type => this.Setter.Parameters[0].Type;
}

internal partial class BoundArrayAccessLvalue
{
    public override TypeSymbol Type => ((ArrayTypeSymbol)this.Array.TypeRequired).ElementType;
}
