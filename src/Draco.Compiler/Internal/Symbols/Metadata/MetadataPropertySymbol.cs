using System.Linq;
using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Class for reading properties from metadata.
/// </summary>
internal sealed class MetadataPropertySymbol : PropertySymbol, IMetadataSymbol
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
            return this.getter;
        }
    }
    private FunctionSymbol? getter;

    public override FunctionSymbol? Setter
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.setter;
        }
    }
    private FunctionSymbol? setter;

    public override bool IsStatic
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.isStatic;
        }
    }
    private bool isStatic = false;

    public override Api.Semantics.Visibility Visibility
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.visibility;
        }
    }
    private Api.Semantics.Visibility visibility = default;

    private bool NeedsBuild => this.type is null;

    public override bool IsIndexer => this.Name == this.defaultMemberName;
    private readonly string? defaultMemberName;

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
    public MetadataPropertySymbol(Symbol containingSymbol, PropertyDefinition propertyDefinition, string? defaultMemberName)
    {
        this.ContainingSymbol = containingSymbol;
        this.propertyDefinition = propertyDefinition;
        this.defaultMemberName = defaultMemberName;
    }

    private void Build()
    {
        var accessors = this.propertyDefinition.GetAccessors();
        if (!accessors.Getter.IsNil)
        {
            var getter = this.MetadataReader.GetMethodDefinition(accessors.Getter);
            this.getter = new MetadataPropertyAccessorSymbol(this.ContainingSymbol, getter, this);
            this.visibility = this.getter.Visibility;
            this.isStatic = this.getter.IsStatic;
            this.type = this.getter.ReturnType;
        }
        if (!accessors.Setter.IsNil)
        {
            var setter = this.MetadataReader.GetMethodDefinition(accessors.Setter);
            this.setter = new MetadataPropertyAccessorSymbol(this.ContainingSymbol, setter, this);
            this.visibility = this.setter.Visibility;
            this.isStatic = this.setter.IsStatic;
            if (this.type is null)
            {
                var decoder = new TypeProvider(this.Assembly.Compilation);
                this.type = setter.DecodeSignature(decoder, default!).ParameterTypes.First();
            }
        }
    }
}
