using System.Diagnostics.CodeAnalysis;

namespace Draco.Compiler.Internal.BoundTree;

[ExcludeFromCodeCoverage]
internal partial class BoundTreeFactory
{
    public static BoundAssignmentExpression AssignmentExpression(BoundLvalue left, BoundExpression right) =>
        AssignmentExpression(null, left, right);
}
