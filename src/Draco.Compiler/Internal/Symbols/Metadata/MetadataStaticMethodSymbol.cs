using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A static method read up from metadata.
/// </summary>
internal sealed class MetadataStaticMethodSymbol : FunctionSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters => this.parameters ??= this.BuildParameters();
    private ImmutableArray<ParameterSymbol>? parameters;

    public override Type ReturnType => this.returnType ??= this.BuildReturnType();
    private Type? returnType;

    public override string Name => this.metadataReader.GetString(this.methodDefinition.Name);

    public override Symbol ContainingSymbol { get; }

    private readonly MethodDefinition methodDefinition;
    private readonly MetadataReader metadataReader;

    public MetadataStaticMethodSymbol(
        Symbol containingSymbol,
        MethodDefinition methodDefinition,
        MetadataReader metadataReader)
    {
        this.ContainingSymbol = containingSymbol;
        this.methodDefinition = methodDefinition;
        this.metadataReader = metadataReader;
    }

    private ImmutableArray<ParameterSymbol> BuildParameters()
    {
        var result = ImmutableArray.CreateBuilder<ParameterSymbol>();

        foreach (var paramHandle in this.methodDefinition.GetParameters())
        {
            var paramDef = this.metadataReader.GetParameter(paramHandle);
            var paramSym = new MetadataParameterSymbol(
                containingSymbol: this,
                parameterDefinition: paramDef,
                metadataReader: this.metadataReader);
            result.Add(paramSym);
        }

        return result.ToImmutable();
    }

    // TODO
    private Type BuildReturnType() => throw new System.NotImplementedException();
}
