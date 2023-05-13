using System.Linq;
using System.Reflection.Metadata;
using System.Reflection;

namespace Draco.Compiler.Internal.Symbols.Metadata;

internal sealed class MetadataPropertySymbol : PropertySymbol
{
    public override TypeSymbol Type
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.type!;
        }
    }
    private TypeSymbol? type;

    public override FunctionSymbol? Getter
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.getter!;
        }
    }
    private FunctionSymbol? getter;

    public override FunctionSymbol? Setter
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.setter!;
        }
    }
    private FunctionSymbol? setter;

    private bool NeedsBuild => this.type is null;

    public override bool IsStatic
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.isStatic;
        }
    }
    private bool isStatic = false;

    public override string Name => this.MetadataReader.GetString(this.propertyDefinition.Name);

    public override Symbol ContainingSymbol { get; }

    /// <summary>
    /// The metadata assembly of this metadata symbol.
    /// </summary>
    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    /// <summary>
    /// The metadata reader that was used to read up this metadata symbol.
    /// </summary>
    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    private readonly PropertyDefinition propertyDefinition;
    public MetadataPropertySymbol(Symbol containingSymbol, PropertyDefinition propertyDefinition)
    {
        this.ContainingSymbol = containingSymbol;
        this.propertyDefinition = propertyDefinition;
    }

    private void Build()
    {
        var accessors = this.propertyDefinition.GetAccessors();
        if (!accessors.Getter.IsNil)
        {
            var getter = this.MetadataReader.GetMethodDefinition(accessors.Getter);
            this.isStatic = getter.Attributes.HasFlag(MethodAttributes.Static);
            this.getter = new MetadataMethodSymbol(this.ContainingSymbol, getter);
            this.type = this.getter.ReturnType;
        }
        if (!accessors.Setter.IsNil)
        {
            var setter = this.MetadataReader.GetMethodDefinition(accessors.Setter);
            this.isStatic = setter.Attributes.HasFlag(MethodAttributes.Static);
            this.setter = new MetadataMethodSymbol(this.ContainingSymbol, setter);
            if (this.type is null)
            {
                var decoder = new SignatureDecoder(this.Assembly.Compilation);
                this.type = setter.DecodeSignature(decoder, default!).ParameterTypes.First();
            }
        }
    }
}
