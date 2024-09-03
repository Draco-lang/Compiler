using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;

namespace Draco.Compiler.Internal.Symbols.Metadata;

// NOTE: This is not abstract or sealed, as this is a legit implementation on its own
// but some symbols will/might reuse this implementation
/// <summary>
/// Utility base-class for methods read up from metadata.
/// </summary>
internal class MetadataMethodSymbol(
    Symbol containingSymbol,
    MethodDefinition methodDefinition) : FunctionSymbol, IMetadataSymbol
{
    public override ImmutableArray<TypeParameterSymbol> GenericParameters =>
        InterlockedUtils.InitializeDefault(ref this.genericParameters, this.BuildGenericParameters);
    private ImmutableArray<TypeParameterSymbol> genericParameters;

    public override ImmutableArray<ParameterSymbol> Parameters
    {
        get
        {
            if (!this.SignatureNeedsBuild) return this.parameters;
            lock (this.signatureBuildLock)
            {
                if (this.SignatureNeedsBuild) this.BuildSignature();
                return this.parameters;
            }
        }
    }
    public override TypeSymbol ReturnType
    {
        get
        {
            if (!this.SignatureNeedsBuild) return this.returnType!;
            lock (this.signatureBuildLock)
            {
                if (this.SignatureNeedsBuild) this.BuildSignature();
                return this.returnType!;
            }
        }
    }

    public override bool IsConstructor =>
           methodDefinition.Attributes.HasFlag(MethodAttributes.SpecialName)
        && this.Name == ".ctor";

    public override bool IsSpecialName => methodDefinition.Attributes.HasFlag(MethodAttributes.SpecialName);

    public override bool IsVirtual
    {
        get
        {
            if (this.IsStatic) return false;
            if (this.ContainingSymbol is TypeSymbol { IsValueType: true }) return false;
            return methodDefinition.Attributes.HasFlag(MethodAttributes.Virtual)
                || this.Override is not null;
        }
    }

    public override bool IsStatic => methodDefinition.Attributes.HasFlag(MethodAttributes.Static);

    // TODO: Very hacky way of doing this
    public override bool IsExplicitImplementation =>
           this.Visibility == Api.Semantics.Visibility.Private
        || this.Name.Contains('.');

    public override Api.Semantics.Visibility Visibility
    {
        get
        {
            // If this is an interface member, default to public
            if (this.ContainingSymbol is TypeSymbol { IsInterface: true })
            {
                return Api.Semantics.Visibility.Public;
            }

            // Otherwise read flag from metadata
            return methodDefinition.Attributes.HasFlag(MethodAttributes.Public)
                ? Api.Semantics.Visibility.Public
                : Api.Semantics.Visibility.Internal;
        }
    }

    public override FunctionSymbol? Override
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
    private FunctionSymbol? @override;
    private volatile bool overrideNeedsBuild = true;
    private readonly object overrideBuildLock = new();

    public override SymbolDocumentation Documentation => LazyInitializer.EnsureInitialized(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    internal override string RawDocumentation => LazyInitializer.EnsureInitialized(ref this.rawDocumentation, this.BuildRawDocumentation);
    private string? rawDocumentation;

    public override Symbol ContainingSymbol { get; } = containingSymbol;

    // IMPORTANT: Choice of flag field because of write order
    private bool SignatureNeedsBuild => Volatile.Read(ref this.returnType) is null;

    private ImmutableArray<ParameterSymbol> parameters;
    private TypeSymbol? returnType;

    public override string Name => this.MetadataName;
    public override string MetadataName => this.MetadataReader.GetString(methodDefinition.Name);

    // NOTE: thread-safety does not matter, same instance
    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    private readonly object signatureBuildLock = new();

    private ImmutableArray<TypeParameterSymbol> BuildGenericParameters()
    {
        var genericParamsHandle = methodDefinition.GetGenericParameters();
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

    private void BuildSignature()
    {
        // Decode signature
        var signature = methodDefinition.DecodeSignature(this.Assembly.Compilation.TypeProvider, this);

        // Build parameters
        var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
        foreach (var (paramHandle, paramType) in methodDefinition.GetParameters().Zip(signature.ParameterTypes))
        {
            var paramDef = this.MetadataReader.GetParameter(paramHandle);
            var paramSym = new MetadataParameterSymbol(
                containingSymbol: this,
                type: paramType,
                parameterDefinition: paramDef);
            parameters.Add(paramSym);
        }
        this.parameters = parameters.ToImmutable();
        // IMPORTANT: returnType is the build flag, needs to be written last
        Volatile.Write(ref this.returnType, signature.ReturnType);
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

    private FunctionSymbol? GetExplicitOverride()
    {
        var type = this.MetadataReader.GetTypeDefinition(methodDefinition.GetDeclaringType());
        foreach (var impl in type.GetMethodImplementations())
        {
            var implementation = this.MetadataReader.GetMethodImplementation(impl);
            var body = MetadataSymbol.GetFunctionFromHandle(implementation.MethodBody, this);

            if (body is null) return null;

            if (!implementation.MethodDeclaration.IsNil && body.CanBeOverriddenBy(this))
            {
                return MetadataSymbol.GetFunctionFromHandle(implementation.MethodDeclaration, this);
            }
        }
        return null;
    }

    private SymbolDocumentation BuildDocumentation() =>
        XmlDocumentationExtractor.Extract(this);

    private string BuildRawDocumentation() =>
        MetadataDocumentation.GetDocumentation(this);
}
