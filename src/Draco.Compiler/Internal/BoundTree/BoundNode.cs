using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

internal partial class BoundUnitExpression
{
    public static BoundUnitExpression Default { get; } = new(null);
}

// Lvalues
