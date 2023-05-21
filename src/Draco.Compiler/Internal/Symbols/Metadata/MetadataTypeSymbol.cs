using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A type definition read up from metadata.
/// </summary>
internal sealed class MetadataTypeSymbol : TypeSymbol
{
    public override IEnumerable<Symbol> Members => this.members ??= this.BuildMembers();
    private ImmutableArray<Symbol>? members;

    public override string Name => this.MetadataReader.GetString(this.typeDefinition.Name);

    public override Symbol ContainingSymbol { get; }
    // TODO: Is this correct?
    public override bool IsValueType => !this.typeDefinition.Attributes.HasFlag(TypeAttributes.Class);

    /// <summary>
    /// The metadata assembly of this metadata symbol.
    /// </summary>
    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    /// <summary>
    /// The metadata reader that was used to read up this metadata symbol.
    /// </summary>
    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    private readonly TypeDefinition typeDefinition;

    public MetadataTypeSymbol(Symbol containingSymbol, TypeDefinition typeDefinition)
    {
        this.ContainingSymbol = containingSymbol;
        this.typeDefinition = typeDefinition;
    }

    public override string ToString() => this.Name;

    private ImmutableArray<Symbol> BuildMembers()
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();

        // Nested types
        foreach (var typeHandle in this.typeDefinition.GetNestedTypes())
        {
            var typeDef = this.MetadataReader.GetTypeDefinition(typeHandle);
            // Skip special name
            if (typeDef.Attributes.HasFlag(TypeAttributes.SpecialName)) continue;
            // Skip non-public
            if (!typeDef.Attributes.HasFlag(TypeAttributes.NestedPublic)) continue;
            Symbol typeSym = typeDef.Attributes.HasFlag(MetadataSymbol.StaticClassAttributes)
                ? new MetadataStaticClassSymbol(this, typeDef)
                : new MetadataTypeSymbol(this, typeDef);
            result.Add(typeSym);
        }

        // Methods
        foreach (var methodHandle in this.typeDefinition.GetMethods())
        {
            var method = this.MetadataReader.GetMethodDefinition(methodHandle);
            // Skip private
            if (method.Attributes.HasFlag(MethodAttributes.Private)) continue;
            // Skip special name
            if (method.Attributes.HasFlag(MethodAttributes.SpecialName)) continue;
            // Add it
            var methodSymbol = new MetadataMethodSymbol(
                containingSymbol: this,
                methodDefinition: method);
            result.Add(methodSymbol);
        }

        // Fields
        foreach (var fieldHandle in this.typeDefinition.GetFields())
        {
            var fieldDef = this.MetadataReader.GetFieldDefinition(fieldHandle);
            // Skip special name
            if (fieldDef.Attributes.HasFlag(FieldAttributes.SpecialName)) continue;
            // Skip non-public
            if (!fieldDef.Attributes.HasFlag(FieldAttributes.Public)) continue;
            // Add it
            var fieldSym = new MetadataFieldSymbol(
                containingSymbol: this,
                fieldDefinition: fieldDef);
            result.Add(fieldSym);
        }

        var defaultName = MetadataSymbol.GetDefaultMemberAttributeName(this.typeDefinition, this.DeclaringCompilation!, this.MetadataReader);

        // Properties
        foreach (var propHandle in this.typeDefinition.GetProperties())
        {
            var propDef = this.MetadataReader.GetPropertyDefinition(propHandle);
            // TODO: visibility
            var propSym = new MetadataPropertySymbol(
                containingSymbol: this,
                propertyDefinition: propDef,
                defaultMemberName: defaultName);
            result.Add(propSym);
        }

        // Done
        return result.ToImmutable();
    }
}
