using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Draco.Compiler.Api;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A static class read up from metadata that we handle as a module.
/// </summary>
internal sealed class MetadataStaticClassSymbol : ModuleSymbol, IMetadataSymbol, IMetadataClass
{
    public override IEnumerable<Symbol> Members
    {
        get
        {
            if (this.members.IsDefault) this.BuildMembers();
            return this.members;
        }
    }

    public ImmutableArray<Symbol> SpecialNameMembers
    {
        get
        {
            if (this.specialNameMembers.IsDefault) this.BuildMembers();
            return this.specialNameMembers;
        }
    }
    private ImmutableArray<Symbol> members;
    private ImmutableArray<Symbol> specialNameMembers;

    public override string Name => this.MetadataName;
    public override string MetadataName => this.MetadataReader.GetString(this.typeDefinition.Name);

    public override Api.Semantics.Visibility Visibility => this.typeDefinition.Attributes.HasFlag(TypeAttributes.Public) ? Api.Semantics.Visibility.Public : Api.Semantics.Visibility.Internal;

    public override Symbol ContainingSymbol { get; }

    // NOTE: thread-safety does not matter, same instance
    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    public override Compilation DeclaringCompilation { get; }

    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    public string? DefaultMemberAttributeName =>
        InterlockedUtils.InitializeMaybeNull(ref this.defaultMemberAttributeName, () => MetadataSymbol.GetDefaultMemberAttributeName(this.typeDefinition, this.DeclaringCompilation!, this.MetadataReader));
    private string? defaultMemberAttributeName;

    private readonly TypeDefinition typeDefinition;

    public MetadataStaticClassSymbol(Symbol containingSymbol, TypeDefinition typeDefinition, Compilation declaringCompilation)
    {
        this.ContainingSymbol = containingSymbol;
        this.typeDefinition = typeDefinition;
        this.DeclaringCompilation = declaringCompilation;
    }

    private void BuildMembers()
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();
        var specialNameResult = ImmutableArray.CreateBuilder<Symbol>();

        // Nested types
        foreach (var typeHandle in this.typeDefinition.GetNestedTypes())
        {
            var typeDef = this.MetadataReader.GetTypeDefinition(typeHandle);
            // Skip non-public
            if (!typeDef.Attributes.HasFlag(TypeAttributes.NestedPublic)) continue;
            var symbols = MetadataSymbol.ToSymbol(this, typeDef, this.MetadataReader, this.DeclaringCompilation);

            if (typeDef.Attributes.HasFlag(TypeAttributes.SpecialName)) specialNameResult.AddRange(symbols);
            else result.AddRange(symbols);
        }

        // Methods
        foreach (var methodHandle in this.typeDefinition.GetMethods())
        {
            var methodDef = this.MetadataReader.GetMethodDefinition(methodHandle);
            // Skip non-public methods
            if (!methodDef.Attributes.HasFlag(MethodAttributes.Public)) continue;
            // Skip non-static methods
            // TODO: What's Invoke in System.Console?
            if (!methodDef.Attributes.HasFlag(MethodAttributes.Static)) continue;
            var methodSym = new MetadataMethodSymbol(
                containingSymbol: this,
                methodDefinition: methodDef);

            if (methodDef.Attributes.HasFlag(MethodAttributes.SpecialName)) specialNameResult.Add(methodSym);
            else result.Add(methodSym);
        }

        // Fields
        foreach (var fieldHandle in this.typeDefinition.GetFields())
        {
            var fieldDef = this.MetadataReader.GetFieldDefinition(fieldHandle);
            // Skip non-public fields
            if (!fieldDef.Attributes.HasFlag(FieldAttributes.Public)) continue;
            // Skip non-static fields
            if (!fieldDef.Attributes.HasFlag(FieldAttributes.Static)) continue;
            var fieldSym = new MetadataFieldSymbol(
                containingSymbol: this,
                fieldDefinition: fieldDef);

            if (fieldDef.Attributes.HasFlag(FieldAttributes.SpecialName)) specialNameResult.Add(fieldSym);
            else result.Add(fieldSym);
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
        this.members = result.ToImmutable();
        this.specialNameMembers = specialNameResult.ToImmutable();
    }
}
