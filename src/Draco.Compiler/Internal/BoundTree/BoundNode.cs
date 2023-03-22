using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Types;

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
    public virtual Type? Type => null;

    public Type TypeRequired => this.Type ?? throw new System.InvalidOperationException();
}

internal partial class BoundUnexpectedExpression
{
    public override Type Type => Intrinsics.Error;
}

internal partial class BoundUnitExpression
{
    public static BoundUnitExpression Default { get; } = new(null);
    public override Type? Type => Intrinsics.Unit;
}

internal partial class BoundGotoExpression
{
    public override Type Type => Intrinsics.Never;
}

internal partial class BoundReturnExpression
{
    public override Type Type => Intrinsics.Never;
}

internal partial class BoundBlockExpression
{
    public override Type Type => this.Value.TypeRequired;
}

internal partial class BoundWhileExpression
{
    public override Type Type => Intrinsics.Unit;
}

internal partial class BoundAndExpression
{
    public override Type Type => Intrinsics.Bool;
}

internal partial class BoundOrExpression
{
    public override Type Type => Intrinsics.Bool;
}

internal partial class BoundParameterExpression
{
    public override Type Type => this.Parameter.Type;
}

internal partial class BoundGlobalExpression
{
    public override Type Type => this.Global.Type;
}

internal partial class BoundLocalExpression
{
    public override Type Type => this.Local.Type;
}

internal partial class BoundReferenceErrorExpression
{
    public override Type Type => Intrinsics.Error;
}

internal partial class BoundLiteralExpression
{
    public override Type Type => this.Value switch
    {
        int => Intrinsics.Int32,
        bool => Intrinsics.Bool,
        _ => throw new System.InvalidOperationException(),
    };
}

internal partial class BoundStringExpression
{
    public override Type Type => Intrinsics.String;
}

internal partial class BoundRelationalExpression
{
    public override Type Type => Intrinsics.Bool;
}

internal partial class BoundAssignmentExpression
{
    public override Type Type => this.Left.Type;
}

// Lvalues

internal partial class BoundLvalue
{
    public abstract Type Type { get; }
}

internal partial class BoundLocalLvalue
{
    public override Type Type => this.Local.Type;
}

internal partial class BoundGlobalLvalue
{
    public override Type Type => this.Global.Type;
}
