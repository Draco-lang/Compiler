using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A default constructor for in-source types.
/// </summary>
internal sealed class DefaultConstructorSymbol : FunctionSymbol
{
    public override TypeSymbol ContainingSymbol { get; }

    public override string Name => ".ctor";
    public override TypeSymbol ReturnType => IntrinsicSymbols.Unit;
    public override bool IsStatic => false;

    public override ImmutableArray<ParameterSymbol> Parameters =>
        InterlockedUtils.InitializeDefault(ref this.parameters, this.BuildParameters);
    private ImmutableArray<ParameterSymbol> parameters;

    public override BoundStatement Body => this.body ??= this.BuildBody();
    private BoundStatement? body;

    public DefaultConstructorSymbol(TypeSymbol containingSymbol)
    {
        this.ContainingSymbol = containingSymbol;
    }

    private ImmutableArray<ParameterSymbol> BuildParameters() =>
        ImmutableArray.Create<ParameterSymbol>(new SynthetizedThisParameterSymbol(this));

    private BoundStatement BuildBody()
    {
        if (this.ContainingSymbol.BaseType is null)
        {
            // No base type, no need to call base constructor
            return ExpressionStatement(ReturnExpression(UnitExpression()));
        }

        // TODO: Filtering for 0 args is not correct
        // while it is fine for metadata functions, for source functions "this" is explicit so it has 1 arg
        // We either kill this asimmetry, or we never make assumptions about the number of args

        // We have a base type, call base constructor
        var defaultCtor = this.ContainingSymbol.BaseType.Constructors
            .FirstOrDefault(ctor => ctor.Parameters.Length == 0);
        // TODO: Error if base has no default constructor?
        if (defaultCtor is null) throw new NotImplementedException();
        return ExpressionStatement(BlockExpression(
            locals: ImmutableArray<LocalSymbol>.Empty,
            statements: ImmutableArray.Create<BoundStatement>(
                ExpressionStatement(CallExpression(
                    receiver: ParameterExpression(this.Parameters[0]),
                    method: defaultCtor,
                    arguments: ImmutableArray<BoundExpression>.Empty)),
                ExpressionStatement(ReturnExpression(BoundUnitExpression.Default))),
            value: BoundUnitExpression.Default));
    }
}
