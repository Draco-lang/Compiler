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
/// An auto-generated getter for an auto-property.
/// </summary>
internal sealed class AutoPropertyGetterSymbol : FunctionSymbol, IPropertyAccessorSymbol
{
    public override TypeSymbol ContainingSymbol { get; }

    public override string Name => $"{this.Property.Name}_Getter";
    public override bool IsStatic => this.Property.IsStatic;
    public override Api.Semantics.Visibility Visibility => this.Property.Visibility;

    public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;
    public override TypeSymbol ReturnType => this.Property.Type;

    public override BoundStatement Body => InterlockedUtils.InitializeNull(ref this.body, this.BuildBody);
    private BoundStatement? body;

    PropertySymbol IPropertyAccessorSymbol.Property => this.Property;
    public SourceAutoPropertySymbol Property { get; }

    public AutoPropertyGetterSymbol(TypeSymbol containingSymbol, SourceAutoPropertySymbol property)
    {
        this.ContainingSymbol = containingSymbol;
        this.Property = property;
    }

    private BoundStatement BuildBody() => ExpressionStatement(BlockExpression(
        locals: ImmutableArray<LocalSymbol>.Empty,
        statements: ImmutableArray.Create<BoundStatement>(
            ExpressionStatement(ReturnExpression(FieldExpression(
                receiver: ParameterExpression(new SynthetizedThisParameterSymbol(this)),
                field: this.Property.BackingField)))),
        value: BoundUnitExpression.Default));
}
