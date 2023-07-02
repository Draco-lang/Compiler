using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A type definition read up from metadata.
/// </summary>
internal sealed class MetadataTypeSymbol : TypeSymbol, IMetadataSymbol, IMetadataClass
{
    public override IEnumerable<Symbol> Members =>
        InterlockedUtils.InitializeDefault(ref this.members, this.BuildMembers);
    private ImmutableArray<Symbol> members;

    public override string Name => InterlockedUtils.InitializeNull(ref this.name, this.BuildName);
    private string? name;

    public override string MetadataName => this.MetadataReader.GetString(this.typeDefinition.Name);

    public override Api.Semantics.Visibility Visibility => this.typeDefinition.Attributes.HasFlag(TypeAttributes.Public) ? Api.Semantics.Visibility.Public : Api.Semantics.Visibility.Internal;

    public override ImmutableArray<TypeParameterSymbol> GenericParameters =>
        InterlockedUtils.InitializeDefault(ref this.genericParameters, this.BuildGenericParameters);
    private ImmutableArray<TypeParameterSymbol> genericParameters;

    public override Symbol? ContainingSymbol { get; }
    // TODO: Is this correct?
    public override bool IsValueType => !this.typeDefinition.Attributes.HasFlag(TypeAttributes.Class);

    public override ImmutableArray<TypeSymbol> BaseTypes => InterlockedUtils.InitializeDefault(ref this.baseTypes, this.BuildBaseTypes);
    private ImmutableArray<TypeSymbol> baseTypes;

    // NOTE: thread-safety does not matter, same instance
    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    public override Compilation DeclaringCompilation { get; }

    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    public string? DefaultMemberAttributeName =>
        InterlockedUtils.InitializeMaybeNull(ref this.defaultMemberAttributeName, () => MetadataSymbol.GetDefaultMemberAttributeName(this.typeDefinition, this.DeclaringCompilation!, this.MetadataReader));
    private string? defaultMemberAttributeName;

    private readonly TypeDefinition typeDefinition;

    public MetadataTypeSymbol(Symbol? containingSymbol, TypeDefinition typeDefinition, Compilation declaringCompilation)
    {
        this.ContainingSymbol = containingSymbol;
        this.typeDefinition = typeDefinition;
        this.DeclaringCompilation = declaringCompilation;
    }

    public override string ToString() => this.GenericParameters.Length == 0
        ? this.Name
        : $"{this.Name}<{string.Join(", ", this.GenericParameters)}>";

    private string BuildName()
    {
        var name = this.MetadataName;
        var backtickIndex = name.IndexOf('`');
        return backtickIndex == -1
            ? name
            : name[..backtickIndex];
    }

    private ImmutableArray<TypeParameterSymbol> BuildGenericParameters()
    {
        var genericParamsHandle = this.typeDefinition.GetGenericParameters();
        if (genericParamsHandle.Count == 0) return ImmutableArray<TypeParameterSymbol>.Empty;

        var result = ImmutableArray.CreateBuilder<TypeParameterSymbol>();
        foreach (var genericParamHandle in genericParamsHandle)
        {
            var genericParam = this.MetadataReader.GetGenericParameter(genericParamHandle);
            var symbol = new MetadataTypeParameterSymbol(this, genericParam);
            result.Add(symbol);
        }
        return result.ToImmutableArray();
    }

    private ImmutableArray<TypeSymbol> BuildBaseTypes()
    {
        if (this.FullName == "System.Object") return ImmutableArray<TypeSymbol>.Empty;
        var builder = ImmutableArray.CreateBuilder<TypeSymbol>();
        var typeProvider = new TypeProvider(this.DeclaringCompilation);
        builder.Add(this.typeDefinition.BaseType.Kind switch
        {
            HandleKind.TypeDefinition => typeProvider.GetTypeFromDefinition(this.MetadataReader, (TypeDefinitionHandle)this.typeDefinition.BaseType, 0),
            HandleKind.TypeReference => typeProvider.GetTypeFromReference(this.MetadataReader, (TypeReferenceHandle)this.typeDefinition.BaseType, 0),
            // TODO: What symbol do we pass there? do we need to pass anything? it works without it
            HandleKind.TypeSpecification => typeProvider.GetTypeFromSpecification(this.MetadataReader, new PrimitiveTypeSymbol("<unknown>", false), (TypeSpecificationHandle)this.typeDefinition.BaseType, 0),
        });
        foreach (var @interface in this.typeDefinition.GetInterfaceImplementations())
        {
            var interfaceDef = this.MetadataReader.GetInterfaceImplementation(@interface);
            builder.Add(interfaceDef.Interface.Kind switch
            {
                HandleKind.TypeDefinition => typeProvider.GetTypeFromDefinition(this.MetadataReader, (TypeDefinitionHandle)interfaceDef.Interface, 0),
                HandleKind.TypeReference => typeProvider.GetTypeFromReference(this.MetadataReader, (TypeReferenceHandle)interfaceDef.Interface, 0),
                // TODO: What symbol do we pass there? do we need to pass anything? it works without it
                HandleKind.TypeSpecification => typeProvider.GetTypeFromSpecification(this.MetadataReader, new PrimitiveTypeSymbol("<unknown>", false), (TypeSpecificationHandle)interfaceDef.Interface, 0),
            });
        }

        return builder.ToImmutable();
    }

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
            var symbols = MetadataSymbol.ToSymbol(this, typeDef, this.MetadataReader, this.DeclaringCompilation);
            result.AddRange(symbols);
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

        // Properties
        foreach (var propHandle in this.typeDefinition.GetProperties())
        {
            var propDef = this.MetadataReader.GetPropertyDefinition(propHandle);
            var propSym = new MetadataPropertySymbol(
                containingSymbol: this,
                propertyDefinition: propDef);
            if (propSym.Visibility == Api.Semantics.Visibility.Public) result.Add(propSym);
        }

        // Done
        return result.ToImmutable();
    }
}
