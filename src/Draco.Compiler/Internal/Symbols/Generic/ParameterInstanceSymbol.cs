using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Generic;

/// <summary>
/// Represents a generic instantiated parameter.
/// The parameter definition itself is not generic, but the parameter was within a generic context.
/// </summary>
internal sealed class ParameterInstanceSymbol : ParameterSymbol, IGenericInstanceSymbol
{
    public override TypeSymbol Type => this.type ??= this.BuildType();
    private TypeSymbol? type;

    public override string Name => this.GenericDefinition.Name;

    public override Symbol? ContainingSymbol { get; }
    public override ParameterSymbol GenericDefinition { get; }

    public GenericContext Context { get; }

    public ParameterInstanceSymbol(Symbol? containingSymbol, ParameterSymbol genericDefinition, GenericContext context)
    {
        this.ContainingSymbol = containingSymbol;
        this.GenericDefinition = genericDefinition;
        this.Context = context;
    }

    private TypeSymbol BuildType() => this.GenericDefinition.Type.GenericInstantiate(this, this.Context);
}
