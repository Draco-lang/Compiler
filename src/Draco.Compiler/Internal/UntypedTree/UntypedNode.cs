using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Types;

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

    public abstract void Accept(UntypedTreeVisitor visitor);
    public abstract TResult Accept<TResult>(UntypedTreeVisitor<TResult> visitor);

    protected static bool Equals<T>(ImmutableArray<T> left, ImmutableArray<T> right)
    {
        if (left.Length != right.Length) return false;
        return left.SequenceEqual(right);
    }
}

// Expressions

internal partial class UntypedExpression
{
    public virtual Type? Type => null;

    public Type TypeRequired => this.Type ?? throw new System.InvalidOperationException();
}

internal partial class UntypedUnitExpression
{
    public static UntypedUnitExpression Default { get; } = new(null);
    public override Type Type => Intrinsics.Unit;
}

internal partial class UntypedGotoExpression
{
    public override Type Type => Intrinsics.Never;
}

internal partial class UntypedReturnExpression
{
    public override Type Type => Intrinsics.Never;
}

internal partial class UntypedBlockExpression
{
    public override Type Type => this.Value.TypeRequired;
}

internal partial class UntypedWhileExpression
{
    public override Type Type => Intrinsics.Unit;
}

internal partial class UntypedAndExpression
{
    public override Type Type => Intrinsics.Bool;
}

internal partial class UntypedOrExpression
{
    public override Type Type => Intrinsics.Bool;
}

internal partial class UntypedParameterExpression
{
    public override Type Type => this.Parameter.Type;
}

internal partial class UntypedFunctionExpression
{
    public override Type Type => this.type ??= this.BuildType();
    private Type? type;

    private Type BuildType() => new FunctionType(
        this.Function.Parameters.Select(p => p.Type).ToImmutableArray(),
        this.Function.ReturnType);
}

internal partial class UntypedLiteralExpression
{
    public override Type Type => this.Value switch
    {
        int => Intrinsics.Int32,
        bool => Intrinsics.Bool,
        _ => throw new System.InvalidOperationException(),
    };
}

internal partial class UntypedRelationalExpression
{
    public override Type Type => Intrinsics.Bool;
}

internal partial class UntypedAssignmentExpression
{
    public override Type Type => this.Left.Type;
}

// Lvalues

internal partial class UntypedLvalue
{
    public abstract Type Type { get; }
}
