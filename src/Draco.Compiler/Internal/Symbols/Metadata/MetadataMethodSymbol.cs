using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Utility base-class for methods read up from metadata.
/// </summary>
internal abstract class MetadataMethodSymbol : FunctionSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.parameters;
        }
    }
    public override TypeSymbol ReturnType
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.returnType!;
        }
    }

    private bool NeedsBuild => this.returnType is null;

    private ImmutableArray<ParameterSymbol> parameters;
    private TypeSymbol? returnType;

    public override string Name => this.metadataReader.GetString(this.methodDefinition.Name);

    private readonly MethodDefinition methodDefinition;
    private readonly MetadataReader metadataReader;

    protected MetadataMethodSymbol(MethodDefinition methodDefinition, MetadataReader metadataReader)
    {
        this.methodDefinition = methodDefinition;
        this.metadataReader = metadataReader;
    }

    private void Build()
    {
        // Decode signature
        var signature = this.methodDefinition.DecodeSignature(SignatureDecoder.Instance, default);

        // Build parameters
        var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
        foreach (var (paramHandle, paramType) in this.methodDefinition.GetParameters().Zip(signature.ParameterTypes))
        {
            var paramDef = this.metadataReader.GetParameter(paramHandle);
            var paramSym = new MetadataParameterSymbol(
                containingSymbol: this,
                type: paramType,
                parameterDefinition: paramDef,
                metadataReader: this.metadataReader);
            parameters.Add(paramSym);
        }
        this.parameters = parameters.ToImmutable();

        // Build return type
        this.returnType = signature.ReturnType;
    }
}
