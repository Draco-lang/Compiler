using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;

namespace Draco.Compiler.Internal.Symbols.Metadata;

// NOTE: This is not abstract or sealed, as this is a legit implementation on its own
// but some symbols will/might reuse this implementation
/// <summary>
/// Utility base-class for methods read up from metadata.
/// </summary>
internal class MetadataMethodSymbol : FunctionSymbol, IMetadataSymbol
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

    public override bool IsMember => !this.methodDefinition.Attributes.HasFlag(MethodAttributes.Static);
    public override bool IsVirtual => this.methodDefinition.Attributes.HasFlag(MethodAttributes.Virtual);
    public override bool IsStatic => this.methodDefinition.Attributes.HasFlag(MethodAttributes.Static);
    public override Api.Semantics.Visibility Visibility => this.methodDefinition.Attributes.HasFlag(MethodAttributes.Public) ? Api.Semantics.Visibility.Public : Api.Semantics.Visibility.Internal;

    public override FunctionSymbol? Override
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
    private FunctionSymbol? @override;
    private bool overrideNeedsBuild = true;

    public override Symbol ContainingSymbol { get; }

    // IMPORTANT: Choice of flag field because of write order
    private bool SignatureNeedsBuild => Volatile.Read(ref this.returnType) is null;

    private ImmutableArray<ParameterSymbol> parameters;
    private TypeSymbol? returnType;

    public override string Name => this.MetadataName;
    public override string MetadataName => this.MetadataReader.GetString(this.methodDefinition.Name);

    // NOTE: thread-safety does not matter, same instance
    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    private readonly MethodDefinition methodDefinition;
    private readonly object signatureBuildLock = new();

    public MetadataMethodSymbol(Symbol containingSymbol, MethodDefinition methodDefinition)
    {
        this.ContainingSymbol = containingSymbol;
        this.methodDefinition = methodDefinition;
    }

    private ImmutableArray<TypeParameterSymbol> BuildGenericParameters()
    {
        var genericParamsHandle = this.methodDefinition.GetGenericParameters();
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

    private void BuildSignature()
    {
        // Decode signature
        var decoder = new TypeProvider(this.Assembly.Compilation);
        var signature = this.methodDefinition.DecodeSignature(decoder, this);

        // Build parameters
        var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
        foreach (var (paramHandle, paramType) in this.methodDefinition.GetParameters().Zip(signature.ParameterTypes))
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

    private FunctionSymbol? GetExplicitOverride()
    {
        var type = this.MetadataReader.GetTypeDefinition(this.methodDefinition.GetDeclaringType());
        foreach (var impl in type.GetMethodImplementations())
        {
            var implementation = this.MetadataReader.GetMethodImplementation(impl);
            var body = implementation.MethodBody.Kind switch
            {
                HandleKind.MethodDefinition => this.GetFunctionFromDefinition((MethodDefinitionHandle)implementation.MethodBody),
                HandleKind.MemberReference => this.GetFunctionFromReference((MemberReferenceHandle)implementation.MethodBody),
                _ => throw new InvalidOperationException(),
            };

            if (body is null) return null;

            if (!implementation.MethodDeclaration.IsNil && body.CanBeOverriddenBy(this)) 
            {
                return implementation.MethodDeclaration.Kind switch
                {
                    HandleKind.MethodDefinition => this.GetFunctionFromDefinition((MethodDefinitionHandle)implementation.MethodDeclaration),
                    HandleKind.MemberReference => this.GetFunctionFromReference((MemberReferenceHandle)implementation.MethodDeclaration),
                    _ => throw new InvalidOperationException(),
                };
            }
        }
        return null;
    }

    private FunctionSymbol? GetFunctionFromDefinition(MethodDefinitionHandle methodDef)
    {
        var definition = this.MetadataReader.GetMethodDefinition(methodDef);
        var provider = new TypeProvider(this.Assembly.Compilation);
        var signature = definition.DecodeSignature(provider, this);
        var type = provider.GetTypeFromDefinition(this.MetadataReader, definition.GetDeclaringType(), 0);
        var symbols = type.DefinedMembers.Concat((type as IMetadataClass)!.PropertyAccessors).OfType<FunctionSymbol>();
        foreach (var function in symbols)
        {
            if (function.Name != this.MetadataReader.GetString(definition.Name)) continue;
            if (CompareSignatures(function, signature)) return function;
        }
        return null;
    }

    private FunctionSymbol? GetFunctionFromReference(MemberReferenceHandle methodRef)
    {
        var reference = this.MetadataReader.GetMemberReference(methodRef);
        var provider = new TypeProvider(this.Assembly.Compilation);
        var signature = reference.DecodeMethodSignature(provider, this);
        var type = provider.GetTypeFromReference(this.MetadataReader, (TypeReferenceHandle)reference.Parent, 0);
        var symbols = type.DefinedMembers.Concat((type as IMetadataClass)!.PropertyAccessors).OfType<FunctionSymbol>();
        foreach (var function in symbols)
        {
            if (function.Name != this.MetadataReader.GetString(reference.Name)) continue;
            if (CompareSignatures(function, signature)) return function;
        }
        return null;
    }

    private static bool CompareSignatures(FunctionSymbol function, MethodSignature<TypeSymbol> signature)
    {
        if (function.Parameters.Length != signature.ParameterTypes.Length) return false;
        if (function.GenericParameters.Length != signature.GenericParameterCount) return false;
        for (int i = 0; i < function.Parameters.Length; i++)
        {
            if (function.Parameters[i].Type.FullName != signature.ParameterTypes[i].FullName) return false;
        }
        return true;
    }
}
