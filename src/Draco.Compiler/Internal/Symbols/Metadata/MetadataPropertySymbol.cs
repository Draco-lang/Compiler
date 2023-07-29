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

    // NOTE: This can lead to re-asking for the accessor, in case there isn't one
    public override FunctionSymbol? Getter => InterlockedUtils.InitializeMaybeNull(ref this.getter, this.BuildGetter);
    private FunctionSymbol? getter;

    // NOTE: This can lead to re-asking for the accessor, in case there isn't one
    public override FunctionSymbol? Setter => InterlockedUtils.InitializeMaybeNull(ref this.setter, this.BuildSetter);
    private FunctionSymbol? setter;

    public override bool IsStatic => (this.Getter ?? this.Setter)?.IsStatic ?? throw new InvalidOperationException();

    public override Api.Semantics.Visibility Visibility => (this.Getter ?? this.Setter)?.Visibility ?? throw new InvalidOperationException();

    public override bool IsIndexer => this.Name == ((IMetadataClass)this.ContainingSymbol).DefaultMemberAttributeName;

    public override string Name => this.MetadataReader.GetString(this.propertyDefinition.Name);

    public override PropertySymbol? Override
    {
        get
        {
            if (this.overrideNeedsBuild)
            {
                this.@override = this.ContainingSymbol is TypeSymbol type
                    ? this.GetExplicitOverride() ?? type.GetOverriddenSymbol(this)
                    : null;
                this.overrideNeedsBuild = false;
            }
            return this.@override;
        }
    }
    private PropertySymbol? @override;
    private bool overrideNeedsBuild = true;

    public override Symbol ContainingSymbol { get; }

    /// <summary>
    /// The metadata assembly of this metadata symbol.
    /// </summary>
    // NOTE: thread-safety does not matter, same instance
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

    private MetadataPropertyAccessorSymbol? BuildGetter()
    {
        var accessors = this.propertyDefinition.GetAccessors();
        if (accessors.Getter.IsNil) return null;
        var getter = this.MetadataReader.GetMethodDefinition(accessors.Getter);
        return new MetadataPropertyAccessorSymbol(this.ContainingSymbol, getter, this);
    }

    private MetadataPropertyAccessorSymbol? BuildSetter()
    {
        var accessors = this.propertyDefinition.GetAccessors();
        if (accessors.Setter.IsNil) return null;
        var setter = this.MetadataReader.GetMethodDefinition(accessors.Setter);
        return new MetadataPropertyAccessorSymbol(this.ContainingSymbol, setter, this);
    }

    private PropertySymbol? GetExplicitOverride()
    {
        // TODO: Take getter or setter, find what it overrides and get the prop
        var accessor = this.Getter ?? this.Setter;
        if (accessor is null) throw new InvalidOperationException();

        if (accessor.Override is not null) return (accessor.Override as IPropertyAccessorSymbol)?.Property;
        return null;
    }
}
