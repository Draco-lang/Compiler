using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Metadata;

// NOTE: This is not abstract or sealed, as this is a legit implementation on its own
// but some symbols (like synthetized constructors) reuse this implementation
/// <summary>
/// Utility base-class for methods read up from metadata.
/// </summary>
internal class MetadataMethodSymbol : FunctionSymbol
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

    public override bool IsMember => !this.methodDefinition.Attributes.HasFlag(MethodAttributes.Static);

    public override Symbol ContainingSymbol { get; }

    private bool NeedsBuild => this.returnType is null;

    private ImmutableArray<ParameterSymbol> parameters;
    private TypeSymbol? returnType;

    public override string Name => this.metadataReader.GetString(this.methodDefinition.Name);

    private readonly MethodDefinition methodDefinition;
    private readonly MetadataReader metadataReader;

    public MetadataMethodSymbol(
        Symbol containingSymbol,
        MethodDefinition methodDefinition,
        MetadataReader metadataReader)
    {
        this.ContainingSymbol = containingSymbol;
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
