using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;


namespace Draco.Compiler.Internal.Symbols.Synthetized;

internal sealed class DefaultConstructorSymbol(TypeSymbol containingSymbol) : FunctionSymbol
{

    public override ImmutableArray<ParameterSymbol> Parameters => [];

    public override TypeSymbol ReturnType { get; } = WellKnownTypes.Unit;

    public override bool IsConstructor => true;
    public override bool IsStatic => false;

    public override string Name => ".ctor";

    public override Symbol? ContainingSymbol { get; } = containingSymbol;

    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;
    public override BoundStatement? Body { get; } = ExpressionStatement(null, ReturnExpression(null, BoundUnitExpression.Default));
}
