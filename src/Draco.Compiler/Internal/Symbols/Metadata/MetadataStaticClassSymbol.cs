using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A static class read up from metadata that we handle as a module.
/// </summary>
internal sealed class MetadataStaticClassSymbol : ModuleSymbol, IMetadataSymbol, IMetadataClass
{
    public override IEnumerable<Symbol> Members =>
        InterlockedUtils.InitializeDefault(ref this.members, this.BuildMembers);
    private ImmutableArray<Symbol> members;

    public override string Name => this.MetadataName;
    public override string MetadataName => this.MetadataReader.GetString(this.typeDefinition.Name);

    public override Api.Semantics.Visibility Visibility => this.typeDefinition.Attributes.HasFlag(TypeAttributes.Public) ? Api.Semantics.Visibility.Public : Api.Semantics.Visibility.Internal;

    public override SymbolDocumentation Documentation => InterlockedUtils.InitializeNull(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    internal override string RawDocumentation => InterlockedUtils.InitializeNull(ref this.rawDocumentation, this.BuildRawDocumentation);
    private string? rawDocumentation;

    public override Symbol ContainingSymbol { get; }

    // NOTE: thread-safety does not matter, same instance
    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    public string? DefaultMemberAttributeName =>
        InterlockedUtils.InitializeMaybeNull(ref this.defaultMemberAttributeName, () => MetadataSymbol.GetDefaultMemberAttributeName(this.typeDefinition, this.Assembly.Compilation, this.MetadataReader));
    private string? defaultMemberAttributeName;

    private readonly TypeDefinition typeDefinition;

    public MetadataStaticClassSymbol(Symbol containingSymbol, TypeDefinition typeDefinition)
    {
        this.ContainingSymbol = containingSymbol;
        this.typeDefinition = typeDefinition;
    }

    private ImmutableArray<Symbol> BuildMembers()
    {
        using var _ = this.Assembly.Compilation.Tracer.Begin($"MetadataStaticClassSymbol({this.Name}).BuildMembers");

        var result = ImmutableArray.CreateBuilder<Symbol>();

        // Nested types
        foreach (var typeHandle in this.typeDefinition.GetNestedTypes())
        {
            var typeDef = this.MetadataReader.GetTypeDefinition(typeHandle);
            // Skip special name
            if (typeDef.Attributes.HasFlag(TypeAttributes.SpecialName)) continue;
            // Skip non-public
            if (!typeDef.Attributes.HasFlag(TypeAttributes.NestedPublic)) continue;
            var symbols = MetadataSymbol.ToSymbol(this, typeDef, this.MetadataReader);
            result.AddRange(symbols);
        }

        // Methods
        foreach (var methodHandle in this.typeDefinition.GetMethods())
        {
            var methodDef = this.MetadataReader.GetMethodDefinition(methodHandle);
            // Skip methods with special name
            if (methodDef.Attributes.HasFlag(MethodAttributes.SpecialName)) continue;
            // Skip non-public methods
            if (!methodDef.Attributes.HasFlag(MethodAttributes.Public)) continue;
            // Skip non-static methods
            // TODO: What's Invoke in System.Console?
            if (!methodDef.Attributes.HasFlag(MethodAttributes.Static)) continue;
            var methodSym = new MetadataMethodSymbol(
                containingSymbol: this,
                methodDefinition: methodDef);
            result.Add(methodSym);
        }

        // Fields
        foreach (var fieldHandle in this.typeDefinition.GetFields())
        {
            var fieldDef = this.MetadataReader.GetFieldDefinition(fieldHandle);
            // Skip fields with special name
            if (fieldDef.Attributes.HasFlag(FieldAttributes.SpecialName)) continue;
            // Skip non-public fields
            if (!fieldDef.Attributes.HasFlag(FieldAttributes.Public)) continue;
            // Skip non-static fields
            if (!fieldDef.Attributes.HasFlag(FieldAttributes.Static)) continue;
            var fieldSym = new MetadataFieldSymbol(
                containingSymbol: this,
                fieldDefinition: fieldDef);
            result.Add(fieldSym);
        }

        // Properties
        foreach (var propHandle in this.typeDefinition.GetProperties())
        {
            var propDef = this.MetadataReader.GetPropertyDefinition(propHandle);
            var propSym = new MetadataPropertySymbol(
                containingSymbol: this,
                propertyDefinition: propDef);
            if (propSym.IsStatic && propSym.Visibility == Api.Semantics.Visibility.Public) result.Add(propSym);
        }

        // Done
        return result.ToImmutable();
    }

    private SymbolDocumentation BuildDocumentation() =>
        XmlDocumentationExtractor.Extract(this);

    private string BuildRawDocumentation() =>
        MetadataSymbol.GetDocumentation(this);
}
