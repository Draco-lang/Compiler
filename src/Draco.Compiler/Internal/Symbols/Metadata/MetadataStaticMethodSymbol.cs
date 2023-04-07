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
    public override ImmutableArray<ParameterSymbol> Parameters
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.parameters;
        }
    }
    public override Type ReturnType
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.returnType;
        }
    }

    private bool NeedsBuild => this.returnType is null;

    private ImmutableArray<ParameterSymbol> parameters;
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
