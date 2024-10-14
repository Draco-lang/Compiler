using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

internal sealed class DefaultConstructorSymbol(TypeSymbol containingSymbol) : FunctionSymbol
{

    public override ImmutableArray<ParameterSymbol> Parameters => [];

    public override TypeSymbol ReturnType { get; } = WellKnownTypes.Unit;

    public override bool IsConstructor => true;

    public override string Name => "Foo";

    public override Symbol? ContainingSymbol { get; } = containingSymbol;

    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;
}
