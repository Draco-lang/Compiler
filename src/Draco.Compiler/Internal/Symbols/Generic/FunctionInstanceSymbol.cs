using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Generic;

/// <summary>
/// Represents a generic instantiated function.
/// </summary>
internal sealed class FunctionInstanceSymbol : FunctionSymbol
{
    public override ImmutableArray<TypeParameterSymbol> GenericParameters => throw new NotImplementedException();
    public override ImmutableArray<ParameterSymbol> Parameters => throw new NotImplementedException();
    public override TypeSymbol ReturnType => throw new NotImplementedException();

    public override bool IsMember => this.GenericDefinition.IsMember;
    public override bool IsVirtual => this.GenericDefinition.IsVirtual;

    public override Symbol? ContainingSymbol => this.GenericDefinition.ContainingSymbol;
    public override FunctionSymbol GenericDefinition { get; }



    public override FunctionSymbol GenericInstantiate(GenericContext context) =>
        throw new NotImplementedException();
}
