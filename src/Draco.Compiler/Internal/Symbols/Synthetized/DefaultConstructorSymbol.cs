using System.Collections.Immutable;
using Draco.Compiler.Internal.BoundTree;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// The default constructor for types without a user-defined constructor.
/// </summary>
internal sealed class DefaultConstructorSymbol(TypeSymbol containingSymbol) : FunctionSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters => [];

    public override TypeSymbol ReturnType { get; } = WellKnownTypes.Unit;

    public override bool IsConstructor => true;
    public override bool IsStatic => false;

    public override string Name => ".ctor";

    public override TypeSymbol ContainingSymbol { get; } = containingSymbol;

    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;
    public override BoundStatement Body { get; } = ExpressionStatement(ReturnExpression(BoundUnitExpression.Default));
}
