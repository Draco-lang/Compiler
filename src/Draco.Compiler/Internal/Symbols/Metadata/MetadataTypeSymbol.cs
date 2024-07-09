using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// A type definition read up from metadata.
/// </summary>
internal sealed class MetadataTypeSymbol(
    Symbol containingSymbol,
    TypeDefinition typeDefinition) : TypeSymbol, IMetadataSymbol, IMetadataClass
{
    public override IEnumerable<Symbol> DefinedMembers =>
        InterlockedUtils.InitializeDefault(ref this.definedMembers, this.BuildMembers);
    private ImmutableArray<Symbol> definedMembers;

    public override string Name => LazyInitializer.EnsureInitialized(ref this.name, this.BuildName);
    private string? name;

    public override string MetadataName => this.MetadataReader.GetString(typeDefinition.Name);

    public override Api.Semantics.Visibility Visibility => typeDefinition.Attributes.HasFlag(TypeAttributes.Public) ? Api.Semantics.Visibility.Public : Api.Semantics.Visibility.Internal;

    public override ImmutableArray<TypeParameterSymbol> GenericParameters =>
        InterlockedUtils.InitializeDefault(ref this.genericParameters, this.BuildGenericParameters);
    private ImmutableArray<TypeParameterSymbol> genericParameters;

    public override SymbolDocumentation Documentation => LazyInitializer.EnsureInitialized(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    internal override string RawDocumentation => LazyInitializer.EnsureInitialized(ref this.rawDocumentation, this.BuildRawDocumentation);
    private string? rawDocumentation;

    public override Symbol ContainingSymbol { get; } = containingSymbol;

    public override bool IsValueType => this.BaseTypes.Contains(
        this.Assembly.Compilation.WellKnownTypes.SystemValueType,
        SymbolEqualityComparer.Default);

    public override bool IsInterface => typeDefinition.Attributes.HasFlag(TypeAttributes.Interface);

    public override bool IsAbstract => typeDefinition.Attributes.HasFlag(TypeAttributes.Abstract);

    public override bool IsSealed => typeDefinition.Attributes.HasFlag(TypeAttributes.Sealed);

    public override ImmutableArray<TypeSymbol> ImmediateBaseTypes => InterlockedUtils.InitializeDefault(ref this.baseTypes, this.BuildBaseTypes);
    private ImmutableArray<TypeSymbol> baseTypes;

    // NOTE: thread-safety does not matter, same instance
    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    public string? DefaultMemberAttributeName =>
        InterlockedUtils.InitializeMaybeNull(ref this.defaultMemberAttributeName, () => MetadataSymbol.GetDefaultMemberAttributeName(typeDefinition, this.Assembly.Compilation, this.MetadataReader));
    private string? defaultMemberAttributeName;

    public IEnumerable<Symbol> AdditionalSymbols =>
        InterlockedUtils.InitializeDefault(ref this.additionalSymbols, this.BuildAdditionalSymbols);
    private ImmutableArray<Symbol> additionalSymbols;

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
        var genericParamsHandle = typeDefinition.GetGenericParameters();
        if (genericParamsHandle.Count == 0) return [];

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
        var builder = ImmutableArray.CreateBuilder<TypeSymbol>();
        var typeProvider = this.Assembly.Compilation.TypeProvider;
        if (!typeDefinition.BaseType.IsNil)
        {
            builder.Add(GetTypeFromMetadata(typeDefinition.BaseType));
        }
        foreach (var @interface in typeDefinition.GetInterfaceImplementations())
        {
            var interfaceDef = this.MetadataReader.GetInterfaceImplementation(@interface);
            if (interfaceDef.Interface.IsNil) continue;
            builder.Add(GetTypeFromMetadata(interfaceDef.Interface));
        }

        return builder.ToImmutable();

        TypeSymbol GetTypeFromMetadata(EntityHandle type) => type.Kind switch
        {
            HandleKind.TypeDefinition => typeProvider!.GetTypeFromDefinition(this.MetadataReader, (TypeDefinitionHandle)type, 0),
            HandleKind.TypeReference => typeProvider!.GetTypeFromReference(this.MetadataReader, (TypeReferenceHandle)type, 0),
            HandleKind.TypeSpecification => typeProvider!.GetTypeFromSpecification(this.MetadataReader, this, (TypeSpecificationHandle)type, 0),
            _ => throw new InvalidOperationException(),
        };
    }

    private ImmutableArray<Symbol> BuildMembers()
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();

        // Nested types
        foreach (var typeHandle in typeDefinition.GetNestedTypes())
        {
            var typeDef = this.MetadataReader.GetTypeDefinition(typeHandle);
            // Skip special name
            if (typeDef.Attributes.HasFlag(TypeAttributes.SpecialName)) continue;
            // Skip non-public
            if (!typeDef.Attributes.HasFlag(TypeAttributes.NestedPublic)) continue;
            // Turn into a symbol
            var symbol = MetadataSymbol.ToSymbol(this, typeDef);
            result.Add(symbol);
            // Add additional symbols
            result.AddRange(((IMetadataClass)symbol).AdditionalSymbols);
        }

        // Methods
        foreach (var methodHandle in typeDefinition.GetMethods())
        {
            var method = this.MetadataReader.GetMethodDefinition(methodHandle);
            // Skip special name, if not a constructor or operator
            if (method.Attributes.HasFlag(MethodAttributes.SpecialName))
            {
                var name = this.MetadataReader.GetString(method.Name);
                if (name != ".ctor" && !name.StartsWith("op_")) continue;
            }
            // Skip private
            if (method.Attributes.HasFlag(MethodAttributes.Private)) continue;
            // Add it
            var methodSymbol = new MetadataMethodSymbol(
                containingSymbol: this,
                methodDefinition: method);
            result.Add(methodSymbol);
        }

        // Fields
        foreach (var fieldHandle in typeDefinition.GetFields())
        {
            var fieldDef = this.MetadataReader.GetFieldDefinition(fieldHandle);
            // Skip special name
            if (fieldDef.Attributes.HasFlag(FieldAttributes.SpecialName)) continue;
            // Skip non-public
            if (!fieldDef.Attributes.HasFlag(FieldAttributes.Public)) continue;
            // Add it
            var fieldSym = fieldDef.Attributes.HasFlag(FieldAttributes.Static)
                ? new MetadataStaticFieldSymbol(containingSymbol: this, fieldDefinition: fieldDef) as Symbol
                : new MetadataFieldSymbol(containingSymbol: this, fieldDefinition: fieldDef);
            result.Add(fieldSym);
        }

        // Properties
        foreach (var propHandle in typeDefinition.GetProperties())
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

    private SymbolDocumentation BuildDocumentation() =>
        XmlDocumentationExtractor.Extract(this);

    private string BuildRawDocumentation() =>
        MetadataSymbol.GetDocumentation(this);

    private ImmutableArray<Symbol> BuildAdditionalSymbols() =>
        MetadataSymbol.GetAdditionalSymbols(this, typeDefinition, this.MetadataReader).ToImmutableArray();
}
