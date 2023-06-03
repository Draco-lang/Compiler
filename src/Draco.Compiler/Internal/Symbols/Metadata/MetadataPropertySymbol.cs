using System;
using System.Linq;
using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Represents properties that are read up from metadata.
/// </summary>
internal sealed class MetadataPropertySymbol : PropertySymbol, IMetadataSymbol
{
    public override TypeSymbol Type => this.Getter?.ReturnType ?? this.Setter?.Parameters[0].Type ?? throw new InvalidOperationException();

    public override FunctionSymbol? Getter
    {
        get
        {
            if (this.getterNeedsBuild) this.getter = this.BuildGetter();
            return this.getter;
        }
    }
    private FunctionSymbol? getter;
    private bool getterNeedsBuild = true;

    public override FunctionSymbol? Setter
    {
        get
        {
            if (this.setterNeedsBuild) this.setter = this.BuildSetter();
            return this.setter;
        }
    }
    private FunctionSymbol? setter;
    private bool setterNeedsBuild = true;

    public override bool IsStatic => (this.Getter ?? this.Setter)?.IsStatic ?? throw new InvalidOperationException();

    public override Api.Semantics.Visibility Visibility => (this.Getter ?? this.Setter)?.Visibility ?? throw new InvalidOperationException();

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

    private MetadataPropertyAccessorSymbol? BuildGetter()
    {
        this.getterNeedsBuild = false;
        var accessors = this.propertyDefinition.GetAccessors();
        if (!accessors.Getter.IsNil)
        {
            var getter = this.MetadataReader.GetMethodDefinition(accessors.Getter);
            return new MetadataPropertyAccessorSymbol(this.ContainingSymbol, getter, this);
        }
        return null;
    }

    private MetadataPropertyAccessorSymbol? BuildSetter()
    {
        this.setterNeedsBuild = false;
        var accessors = this.propertyDefinition.GetAccessors();
        if (!accessors.Setter.IsNil)
        {
            var setter = this.MetadataReader.GetMethodDefinition(accessors.Setter);
            return new MetadataPropertyAccessorSymbol(this.ContainingSymbol, setter, this);
        }
        return null;
    }
}
