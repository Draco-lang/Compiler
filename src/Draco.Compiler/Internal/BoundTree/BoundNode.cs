using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.BoundTree;

/// <summary>
/// The base for all bound nodes in the bound tree.
/// </summary>
[ExcludeFromCodeCoverage]
internal abstract partial class BoundNode(SyntaxNode? syntax)
{
    public SyntaxNode? Syntax { get; } = syntax;

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

    public TypeSymbol TypeRequired => this.Type ?? WellKnownTypes.ErrorType;
}

internal partial class BoundUnexpectedExpression
{
    public override TypeSymbol Type => WellKnownTypes.ErrorType;
}

internal partial class BoundSequencePointExpression
{
    public override TypeSymbol? Type => this.Expression.Type;
}

internal partial class BoundUnitExpression
{
    public static BoundUnitExpression Default { get; } = new(null);
    public override TypeSymbol Type => WellKnownTypes.Unit;
}

internal partial class BoundGotoExpression
{
    public override TypeSymbol Type => WellKnownTypes.Never;
}

internal partial class BoundReturnExpression
{
    public override TypeSymbol Type => WellKnownTypes.Never;
}

internal partial class BoundBlockExpression
{
    public override TypeSymbol Type => this.Value.TypeRequired;
}

internal partial class BoundWhileExpression
{
    public override TypeSymbol Type => WellKnownTypes.Unit;
}

internal partial class BoundForExpression
{
    public override TypeSymbol Type => WellKnownTypes.Unit;
}

internal partial class BoundParameterExpression
{
    public override TypeSymbol Type => this.Parameter.Type;
}

internal partial class BoundFieldExpression
{
    public override TypeSymbol Type => this.Field.Type;
}

internal partial class BoundPropertyGetExpression
{
    public override TypeSymbol Type => this.Getter.ReturnType;
}

internal partial class BoundPropertySetExpression
{
    public override TypeSymbol? Type => this.Value.Type;
}

internal partial class BoundIndexGetExpression
{
    public override TypeSymbol? Type => this.Getter.ReturnType;
}

internal partial class BoundIndexSetExpression
{
    public override TypeSymbol? Type => this.Value.Type;
}

internal partial class BoundLocalExpression
{
    public override TypeSymbol Type => this.Local.Type;
}

internal partial class BoundReferenceErrorExpression
{
    public override TypeSymbol Type => WellKnownTypes.ErrorType;
}

internal partial class BoundAndExpression
{
    public override TypeSymbol Type => this.Left.TypeRequired;
}

internal partial class BoundOrExpression
{
    public override TypeSymbol Type => this.Left.TypeRequired;
}

internal partial class BoundAssignmentExpression
{
    public override TypeSymbol Type => this.Left.Type;
}

internal partial class BoundObjectCreationExpression
{
    public override TypeSymbol Type => this.ObjectType;
}

internal partial class BoundDelegateCreationExpression
{
    public override TypeSymbol Type => (TypeSymbol)this.DelegateConstructor.ContainingSymbol!;
}

internal partial class BoundArrayAccessExpression
{
    public override TypeSymbol Type => this.Array.TypeRequired.GenericArguments.FirstOrDefault()
                                    ?? WellKnownTypes.ErrorType;
}

internal partial class BoundCallExpression
{
    public override TypeSymbol Type => this.Method.ReturnType;
}

internal partial class BoundUnaryExpression
{
    public override TypeSymbol Type => this.Operator.ReturnType;
}

internal partial class BoundBinaryExpression
{
    public override TypeSymbol Type => this.Operator.ReturnType;
}

// Lvalues

internal partial class BoundLvalue
{
    public abstract TypeSymbol Type { get; }
}

internal partial class BoundUnexpectedLvalue
{
    public override TypeSymbol Type => WellKnownTypes.ErrorType;
}

internal partial class BoundIllegalLvalue
{
    public override TypeSymbol Type => WellKnownTypes.ErrorType;
}

internal partial class BoundLocalLvalue
{
    public override TypeSymbol Type => this.Local.Type;
}

internal partial class BoundFieldLvalue
{
    public override TypeSymbol Type => this.Field.Type;
}

internal partial class BoundArrayAccessLvalue
{
    public override TypeSymbol Type => this.Array.TypeRequired.GenericArguments[0];
}

internal partial class BoundPropertySetLvalue
{
    public override TypeSymbol Type => ((IPropertyAccessorSymbol)this.Setter).Property.Type;
}

internal partial class BoundIndexSetLvalue
{
    public override TypeSymbol Type => ((IPropertyAccessorSymbol)this.Setter).Property.Type;
}
