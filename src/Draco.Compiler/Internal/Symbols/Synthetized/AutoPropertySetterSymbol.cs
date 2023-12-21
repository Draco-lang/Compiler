using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols.Source;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// An auto-generated setter for an auto-property.
/// </summary>
internal sealed class AutoPropertySetterSymbol : FunctionSymbol, IPropertyAccessorSymbol
{
    public override TypeSymbol ContainingSymbol { get; }

    public override string Name => $"{this.Property.Name}_Setter";
    public override bool IsStatic => this.Property.IsStatic;
    public override Api.Semantics.Visibility Visibility => this.Property.Visibility;

    public override ImmutableArray<ParameterSymbol> Parameters => InterlockedUtils.InitializeDefault(ref this.parameters, this.BuildParameters);
    private ImmutableArray<ParameterSymbol> parameters;

    public override TypeSymbol ReturnType => IntrinsicSymbols.Unit;

    public override BoundStatement Body => InterlockedUtils.InitializeNull(ref this.body, this.BuildBody);
    private BoundStatement? body;

    PropertySymbol IPropertyAccessorSymbol.Property => this.Property;
    public SourceAutoPropertySymbol Property { get; }

    public AutoPropertySetterSymbol(TypeSymbol containingSymbol, SourceAutoPropertySymbol property)
    {
        this.ContainingSymbol = containingSymbol;
        this.Property = property;
    }

    private ImmutableArray<ParameterSymbol> BuildParameters() =>
        ImmutableArray.Create<ParameterSymbol>(new SynthetizedParameterSymbol(this, "value", this.Property.Type));

    private BoundStatement BuildBody() => ExpressionStatement(BlockExpression(
        locals: ImmutableArray<LocalSymbol>.Empty,
        statements: ImmutableArray.Create<BoundStatement>(
            ExpressionStatement(AssignmentExpression(
                compoundOperator: null,
                left: FieldLvalue(
                    receiver: ParameterExpression(new SynthetizedThisParameterSymbol(this)),
                    field: this.Property.BackingField),
                right: ParameterExpression(this.Parameters[0]))),
            ExpressionStatement(ReturnExpression(BoundUnitExpression.Default))),
        value: BoundUnitExpression.Default));
}
