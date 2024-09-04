using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Represents properties that are read up from metadata.
/// </summary>
internal sealed class MetadataPropertySymbol(
    Symbol containingSymbol,
    PropertyDefinition propertyDefinition) : PropertySymbol, IMetadataSymbol
{
    public override ImmutableArray<AttributeInstance> Attributes => InterlockedUtils.InitializeDefault(ref this.attributes, this.BuildAttributes);
    private ImmutableArray<AttributeInstance> attributes;

    public override TypeSymbol Type => this.Getter?.ReturnType ?? this.Setter?.Parameters[0].Type ?? throw new InvalidOperationException();

    // NOTE: This can lead to re-asking for the accessor, in case there isn't one
    public override FunctionSymbol? Getter => InterlockedUtils.InitializeMaybeNull(ref this.getter, this.BuildGetter);
    private FunctionSymbol? getter;

    // NOTE: This can lead to re-asking for the accessor, in case there isn't one
    public override FunctionSymbol? Setter => InterlockedUtils.InitializeMaybeNull(ref this.setter, this.BuildSetter);
    private FunctionSymbol? setter;

    public override bool IsStatic => (this.Getter ?? this.Setter)?.IsStatic ?? throw new InvalidOperationException();
    public override bool IsExplicitImplementation => this.Getter?.IsExplicitImplementation
                                                  ?? this.Setter?.IsExplicitImplementation
                                                  ?? false;

    public override Api.Semantics.Visibility Visibility => (this.Getter ?? this.Setter)?.Visibility ?? throw new InvalidOperationException();

    public override bool IsIndexer => this.Name == this.DefaultMemberName;

    private string DefaultMemberName
    {
        get
        {
            var defaultMemberAttrType = this.Assembly.DeclaringCompilation.WellKnownTypes.SystemReflectionDefaultMemberAttribute;
            var defaultMemberAttrib = this.GetAttribute<System.Reflection.DefaultMemberAttribute>(defaultMemberAttrType);
            return defaultMemberAttrib?.MemberName ?? CompilerConstants.DefaultMemberName;
        }
    }

    public override string Name => this.MetadataReader.GetString(propertyDefinition.Name);

    public override PropertySymbol? Override
    {
        get
        {
            if (!this.overrideNeedsBuild) return this.@override;
            lock (this.overrideBuildLock)
            {
                if (this.overrideNeedsBuild) this.BuildOverride();
                return this.@override;
            }
        }
    }
    private PropertySymbol? @override;
    private volatile bool overrideNeedsBuild = true;
    private readonly object overrideBuildLock = new();

    public override SymbolDocumentation Documentation => LazyInitializer.EnsureInitialized(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    internal override string RawDocumentation => LazyInitializer.EnsureInitialized(ref this.rawDocumentation, this.BuildRawDocumentation);
    private string? rawDocumentation;

    public override Symbol ContainingSymbol { get; } = containingSymbol;

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

    private ImmutableArray<AttributeInstance> BuildAttributes() =>
        MetadataSymbol.DecodeAttributeList(propertyDefinition.GetCustomAttributes(), this);

    private MetadataPropertyAccessorSymbol? BuildGetter()
    {
        var accessors = propertyDefinition.GetAccessors();
        if (accessors.Getter.IsNil) return null;
        var getter = this.MetadataReader.GetMethodDefinition(accessors.Getter);
        return new MetadataPropertyAccessorSymbol(this.ContainingSymbol, getter, this);
    }

    private MetadataPropertyAccessorSymbol? BuildSetter()
    {
        var accessors = propertyDefinition.GetAccessors();
        if (accessors.Setter.IsNil) return null;
        var setter = this.MetadataReader.GetMethodDefinition(accessors.Setter);
        return new MetadataPropertyAccessorSymbol(this.ContainingSymbol, setter, this);
    }

    private void BuildOverride()
    {
        var explicitOverride = this.GetExplicitOverride();
        this.@override = this.ContainingSymbol is TypeSymbol type
            ? explicitOverride ?? type.GetOverriddenSymbol(this)
            : null;
        // IMPORTANT: Write flag last
        this.overrideNeedsBuild = false;
    }

    private PropertySymbol? GetExplicitOverride()
    {
        var accessor = this.Getter ?? this.Setter ?? throw new InvalidOperationException();
        if (accessor.Override is not null) return (accessor.Override as IPropertyAccessorSymbol)?.Property;
        return null;
    }

    private SymbolDocumentation BuildDocumentation() =>
        XmlDocumentationExtractor.Extract(this);

    private string BuildRawDocumentation() =>
        MetadataDocumentation.GetDocumentation(this);
}
