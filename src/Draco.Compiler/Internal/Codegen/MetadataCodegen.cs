using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Metadata;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Generates metadata.
/// </summary>
internal sealed class MetadataCodegen : MetadataWriter
{
    public static void Generate(Compilation compilation, IAssembly assembly, Stream peStream, Stream? pdbStream)
    {
        var codegen = new MetadataCodegen(
            compilation: compilation,
            assembly: assembly,
            writePdb: pdbStream is not null);
        codegen.EncodeAssembly();
        codegen.WritePe(peStream);
        if (pdbStream is not null) codegen.PdbCodegen!.WritePdb(pdbStream);
    }

    public static byte[] MicrosoftPublicKeyToken { get; } = new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a };

    /// <summary>
    /// The compilation that started codegen.
    /// </summary>
    public Compilation Compilation { get; }

    /// <summary>
    /// The PDB code-generator, in case we generate PDBs.
    /// </summary>
    public PdbCodegen? PdbCodegen { get; }

    /// <summary>
    /// The handle for the written module.
    /// </summary>
    public ModuleDefinitionHandle ModuleDefinitionHandle { get; private set; }

    /// <summary>
    /// The handle for the written assembly.
    /// </summary>
    public AssemblyDefinitionHandle AssemblyDefinitionHandle { get; private set; }

    /// <summary>
    /// Handle for the entry point.
    /// </summary>
    public MethodDefinitionHandle EntryPointHandle { get; private set; }

    private WellKnownTypes WellKnownTypes => this.Compilation.WellKnownTypes;

    private readonly IAssembly assembly;
    private readonly BlobBuilder ilBuilder = new();
    private readonly Dictionary<Global, MemberReferenceHandle> globalReferenceHandles = new();
    private readonly Dictionary<IProcedure, MemberReferenceHandle> procedureReferenceHandles = new();
    private readonly Dictionary<IModule, TypeReferenceHandle> moduleReferenceHandles = new();
    private readonly Dictionary<Symbol, MemberReferenceHandle> intrinsicReferenceHandles = new();
    private AssemblyReferenceHandle systemRuntimeReference;
    private TypeReferenceHandle systemObjectReference;

    private MetadataCodegen(Compilation compilation, IAssembly assembly, bool writePdb)
    {
        this.Compilation = compilation;
        if (writePdb) this.PdbCodegen = new(this);
        this.assembly = assembly;
        this.LoadIntrinsics();
        this.WriteModuleAndAssemblyDefinition();

        // Reference System.Object from System.Runtime
        this.systemRuntimeReference = this.GetOrAddAssemblyReference(
            name: "System.Runtime",
            version: new System.Version(7, 0, 0, 0),
            publicKeyOrToken: MicrosoftPublicKeyToken);

        this.systemObjectReference = this.GetOrAddTypeReference(
          assembly: this.systemRuntimeReference,
          @namespace: "System",
          name: "Object");
    }

    private void LoadIntrinsics()
    {
    }

    private void WriteModuleAndAssemblyDefinition()
    {
        var assemblyName = this.assembly.Name;
        var moduleName = Path.ChangeExtension(assemblyName, ".dll");
        this.ModuleDefinitionHandle = this.AddModuleDefinition(
            generation: 0,
            name: moduleName,
            // TODO: Proper module-version ID
            moduleVersionId: Guid.NewGuid());
        this.AssemblyDefinitionHandle = this.AddAssemblyDefinition(
            name: assemblyName,
            // TODO: Proper versioning
            version: new(1, 0, 0, 0));

        // Create type definition for the special <Module> type that holds global functions
        // Note, that we don't use that for our free-functions
        this.AddTypeDefinition(
            attributes: default,
            @namespace: default,
            name: "<Module>",
            baseType: default,
            fieldList: MetadataTokens.FieldDefinitionHandle(1),
            methodList: MetadataTokens.MethodDefinitionHandle(1));
    }

    public MemberReferenceHandle GetGlobalReferenceHandle(Global global)
    {
        if (!this.globalReferenceHandles.TryGetValue(global, out var handle))
        {
            // Add the field reference
            handle = this.AddMemberReference(
                parent: this.GetModuleReferenceHandle(global.DeclaringModule),
                name: global.Name,
                signature: this.EncodeGlobalSignature(global));
            // Cache
            this.globalReferenceHandles.Add(global, handle);
        }
        return handle;
    }

    public MemberReferenceHandle GetProcedureReferenceHandle(IProcedure procedure)
    {
        if (!this.procedureReferenceHandles.TryGetValue(procedure, out var handle))
        {
            var signature = this.EncodeProcedureSignature(procedure);
            handle = this.AddMemberReference(
                parent: this.GetModuleReferenceHandle(procedure.DeclaringModule),
                name: procedure.Name,
                signature: signature);
            this.procedureReferenceHandles.Add(procedure, handle);
        }
        return handle;
    }

    public TypeReferenceHandle GetModuleReferenceHandle(IModule module)
    {
        if (!this.moduleReferenceHandles.TryGetValue(module, out var handle))
        {
            var resolutionScope = module.Parent is null
                // Root module, we take the module definition containing it
                ? (EntityHandle)this.ModuleDefinitionHandle
                // We take its parent module
                : this.GetModuleReferenceHandle(module.Parent);
            var name = string.IsNullOrEmpty(module.Name)
                ? CompilerConstants.DefaultModuleName
                : module.Name;
            handle = this.GetOrAddTypeReference(
                parent: resolutionScope,
                @namespace: null,
                name: name);
            this.moduleReferenceHandles.Add(module, handle);
        }
        return handle;
    }

    public UserStringHandle GetStringLiteralHandle(string text) => this.MetadataBuilder.GetOrAddUserString(text);

    public MemberReferenceHandle GetIntrinsicReferenceHandle(Symbol symbol) => this.intrinsicReferenceHandles[symbol];

    // TODO: This can be cached by symbol to avoid double reference instertion
    public EntityHandle GetEntityHandle(Symbol symbol)
    {
        switch (symbol)
        {
        // If we can translate a symbol to a metadata type, get the handle for that
        // This is because primitives are encoded differently as an entity handle
        case Symbol when this.WellKnownTypes.TryTranslateIntrinsicToMetadataSymbol(symbol, out var metadataSymbol):
            return this.GetEntityHandle(metadataSymbol);

        case MetadataAssemblySymbol assembly:
            return this.AddAssemblyReference(assembly);

        // Metadata types
        case IMetadataSymbol metadataSymbol when metadataSymbol is TypeSymbol or MetadataStaticClassSymbol:
            Debug.Assert(symbol.ContainingSymbol is not null);
            return this.GetOrAddTypeReference(
                parent: this.GetContainerEntityHandle(symbol.ContainingSymbol),
                @namespace: GetNamespaceForSymbol(symbol),
                name: metadataSymbol.MetadataName);

        // Generic type instance
        case TypeSymbol typeSymbol when typeSymbol.IsGenericInstance:
        {
            Debug.Assert(typeSymbol.GenericDefinition is not null);
            var blob = this.EncodeBlob(e =>
            {
                var encoder = e.TypeSpecificationSignature();
                var typeRef = this.GetEntityHandle(typeSymbol.GenericDefinition);
                var argsEncoder = encoder.GenericInstantiation(
                    genericType: typeRef,
                    genericArgumentCount: typeSymbol.GenericArguments.Length,
                    isValueType: typeSymbol.IsValueType);
                foreach (var arg in typeSymbol.GenericArguments)
                {
                    this.EncodeSignatureType(argsEncoder.AddArgument(), arg);
                }
            });
            return this.MetadataBuilder.AddTypeSpecification(blob);
        }

        // Generic function instance
        case FunctionSymbol func when func.IsGenericInstance:
        {
            Debug.Assert(func.GenericDefinition is not null);
            var blob = this.EncodeBlob(e =>
            {
                var encoder = e.MethodSpecificationSignature(func.GenericArguments.Length);
                foreach (var arg in func.GenericArguments)
                {
                    this.EncodeSignatureType(encoder.AddArgument(), arg);
                }
            });
            var genericDef = this.GetEntityHandle(func.GenericDefinition);
            return this.MetadataBuilder.AddMethodSpecification(genericDef, blob);
        }

        // Nongeneric function
        case FunctionSymbol func:
        {
            var isInGenericInstance = func.ContainingSymbol?.IsGenericInstance ?? false;
            return this.AddMemberReference(
                // TODO: Should a function ever have a null container?
                // Probably not, let's shove them somewhere known once we can make up our minds
                // This is the case for synthetized ctor functions for example
                parent: func.ContainingSymbol is null || (func.ContainingSymbol is PropertySymbol && func.ContainingSymbol.ContainingSymbol is null)
                    ? this.GetModuleReferenceHandle(this.assembly.RootModule)
                    : this.GetEntityHandle(func.ContainingSymbol is PropertySymbol
                        ? func.ContainingSymbol.ContainingSymbol!
                        : func.ContainingSymbol),
                name: func.Name,
                signature: this.EncodeBlob(e =>
                {
                    // In generic instances we still need to reference the generic types
                    if (isInGenericInstance) func = func.GenericDefinition!;
                    e
                        .MethodSignature(
                            genericParameterCount: func.GenericParameters.Length,
                            isInstanceMethod: func.IsMember)
                        .Parameters(func.Parameters.Length, out var returnType, out var parameters);
                    this.EncodeReturnType(returnType, func.ReturnType);
                    foreach (var param in func.Parameters)
                    {
                        this.EncodeSignatureType(parameters.AddParameter().Type(), param.Type);
                    }
                }));
        }

        case SourceModuleSymbol module:
        {
            var irModule = this.assembly.Lookup(module);
            return this.GetModuleReferenceHandle(irModule);
        }

        default:
            throw new ArgumentOutOfRangeException(nameof(symbol));
        }
    }

    private EntityHandle GetContainerEntityHandle(Symbol symbol) => symbol switch
    {
        MetadataNamespaceSymbol ns => this.GetContainerEntityHandle(ns.ContainingSymbol),
        MetadataAssemblySymbol module => this.AddAssemblyReference(module),
        MetadataTypeSymbol type => this.GetOrAddTypeReference(
            assembly: this.AddAssemblyReference(type.Assembly),
            @namespace: GetNamespaceForSymbol(type),
            name: type.Name),
        _ => throw new ArgumentOutOfRangeException(nameof(symbol)),
    };

    private AssemblyReferenceHandle AddAssemblyReference(MetadataAssemblySymbol module) =>
        this.GetOrAddAssemblyReference(
            name: module.Name,
            version: new(1, 0)); // TODO: What version?

    private static string? GetNamespaceForSymbol(Symbol symbol) => symbol switch
    {
        MetadataStaticClassSymbol staticClass => GetNamespaceForSymbol(staticClass.ContainingSymbol),
        MetadataTypeSymbol type => GetNamespaceForSymbol(type.ContainingSymbol),
        MetadataNamespaceSymbol ns => ns.FullName,
        _ when symbol.ContainingSymbol is not null => GetNamespaceForSymbol(symbol.ContainingSymbol),
        _ => throw new ArgumentOutOfRangeException(nameof(symbol)),
    };

    private void EncodeAssembly()
    {
        this.EncodeModule(this.assembly.RootModule);

        // If we write a PDB, we add the debuggable attribute to the assembly
        if (this.PdbCodegen is not null)
        {
            var debuggableAttribute = this.GetOrAddTypeReference(
                assembly: this.systemRuntimeReference,
                @namespace: "System.Diagnostics",
                name: "DebuggableAttribute");
            var debuggingModes = this.GetOrAddTypeReference(
                containingType: debuggableAttribute,
                @namespace: "System.Diagnostics",
                name: "DebuggingModes");
            var debuggableAttributeCtor = this.AddMemberReference(
                parent: debuggableAttribute,
                name: ".ctor",
                signature: this.EncodeBlob(e =>
                {
                    e.MethodSignature().Parameters(1, out var returnType, out var parameters);
                    returnType.Void();
                    parameters.AddParameter().Type().Type(debuggingModes, true);
                }));
            this.AddAttribute(
                target: this.AssemblyDefinitionHandle,
                ctor: debuggableAttributeCtor,
                value: this.GetOrAddBlob(new byte[] { 01, 00, 07, 01, 00, 00, 00, 00 }));
        }
    }

    private void EncodeModule(IModule module, TypeDefinitionHandle? parentModule = null, int fieldIndex = 1, int procIndex = 1)
    {
        var currentFieldIndex = fieldIndex;
        var currentProcIndex = procIndex;
        // Go through globals
        foreach (var global in module.Globals.Values)
        {
            this.EncodeGlobal(global);
            currentFieldIndex++;
        }

        // Go through procedures
        foreach (var procedure in module.Procedures.Values)
        {
            // Global initializer will get special treatment
            if (ReferenceEquals(module.GlobalInitializer, procedure)) continue;

            // Encode the procedure
            var handle = this.EncodeProcedure(procedure);

            // If this is the entry point, save it
            if (ReferenceEquals(this.assembly.EntryPoint, procedure)) this.EntryPointHandle = handle;
            currentProcIndex++;
        }

        // Compile global initializer too
        this.EncodeProcedure(module.GlobalInitializer, specialName: ".cctor");
        currentProcIndex++;

        TypeAttributes visibility;
        if (module.Symbol.Visibility == Api.Semantics.Visibility.Public)
        {
            visibility = parentModule is not null ? TypeAttributes.NestedPublic : TypeAttributes.Public;
        }
        else
        {
            visibility = parentModule is not null ? TypeAttributes.NestedAssembly : TypeAttributes.NotPublic;
        }
        var attributes = visibility | TypeAttributes.Class | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit | TypeAttributes.Abstract | TypeAttributes.Sealed;

        var name = string.IsNullOrEmpty(module.Name) ? CompilerConstants.DefaultModuleName : module.Name;

        // Create the type
        var createdModule = this.AddTypeDefinition(
            attributes: attributes,
            @namespace: default,
            name: name,
            baseType: this.systemObjectReference,
            fieldList: MetadataTokens.FieldDefinitionHandle(fieldIndex),
            methodList: MetadataTokens.MethodDefinitionHandle(procIndex));

        // If this isn't top level module, we specify nested relationship
        if (parentModule is not null) this.MetadataBuilder.AddNestedType(createdModule, parentModule.Value);

        // We encode every submodule
        foreach (var subModule in module.Submodules.Values)
        {
            this.EncodeModule(subModule, createdModule, currentFieldIndex, currentProcIndex);
        }
    }

    private FieldDefinitionHandle EncodeGlobal(Global global)
    {
        var visibility = global.Symbol.Visibility switch
        {
            Api.Semantics.Visibility.Public => FieldAttributes.Public,
            Api.Semantics.Visibility.Internal => FieldAttributes.Assembly,
            Api.Semantics.Visibility.Private => FieldAttributes.Private,
            _ => throw new ArgumentOutOfRangeException(nameof(global.Symbol.Visibility)),
        };

        // Definition
        return this.AddFieldDefinition(
            attributes: visibility | FieldAttributes.Static,
            name: global.Name,
            signature: this.EncodeGlobalSignature(global));
    }

    private MethodDefinitionHandle EncodeProcedure(IProcedure procedure, string? specialName = null)
    {
        var visibility = procedure.Symbol.Visibility switch
        {
            Api.Semantics.Visibility.Public => MethodAttributes.Public,
            Api.Semantics.Visibility.Internal => MethodAttributes.Assembly,
            Api.Semantics.Visibility.Private => MethodAttributes.Private,
            _ => throw new ArgumentOutOfRangeException(nameof(procedure.Symbol.Visibility)),
        };

        // Encode instructions
        var cilCodegen = new CilCodegen(this, procedure);
        cilCodegen.EncodeProcedure();

        // Encode locals
        var allocatedLocals = cilCodegen.AllocatedLocals.ToImmutableArray();
        var localsHandle = this.EncodeLocals(allocatedLocals);

        // Encode body
        this.ilBuilder.Align(4);
        var encoder = new MethodBodyStreamEncoder(this.ilBuilder);
        var methodBodyOffset = encoder.AddMethodBody(
            instructionEncoder: cilCodegen.InstructionEncoder,
            // Since we don't do stackification yet, 8 is fine
            // TODO: This is where the stackification optimization step could help to reduce local allocation
            maxStack: 8,
            localVariablesSignature: localsHandle,
            attributes: MethodBodyAttributes.None,
            hasDynamicStackAllocation: false);

        // Determine attributes
        var attributes = MethodAttributes.Static | MethodAttributes.HideBySig;
        attributes |= specialName is null
            ? visibility
            : MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        // Parameters
        var parameterList = this.NextParameterHandle;
        foreach (var param in procedure.ParametersInDefinitionOrder)
        {
            this.AddParameterDefinition(
                attributes: ParameterAttributes.None,
                name: param.Name,
                index: param.Index);
        }

        // Add definition
        var definitionHandle = this.MetadataBuilder.AddMethodDefinition(
            attributes: attributes,
            implAttributes: MethodImplAttributes.IL,
            name: this.GetOrAddString(specialName ?? procedure.Name),
            signature: this.EncodeProcedureSignature(procedure),
            bodyOffset: methodBodyOffset,
            parameterList: parameterList);

        // Add generic type parameters
        var genericIndex = 0;
        foreach (var typeParam in procedure.Generics)
        {
            this.MetadataBuilder.AddGenericParameter(
                parent: definitionHandle,
                attributes: GenericParameterAttributes.None,
                name: this.GetOrAddString(typeParam.Name),
                index: genericIndex++);
        }

        // Write out any debug information
        this.PdbCodegen?.EncodeProcedureDebugInfo(procedure, definitionHandle);

        return definitionHandle;
    }

    private BlobHandle EncodeGlobalSignature(Global global) =>
        this.EncodeBlob(e => this.EncodeSignatureType(e.Field().Type(), global.Type));

    private BlobHandle EncodeProcedureSignature(IProcedure procedure) => this.EncodeBlob(e =>
    {
        e
            .MethodSignature(genericParameterCount: procedure.Generics.Count)
            .Parameters(procedure.Parameters.Count, out var retEncoder, out var paramsEncoder);
        this.EncodeReturnType(retEncoder, procedure.ReturnType);
        foreach (var param in procedure.ParametersInDefinitionOrder)
        {
            this.EncodeSignatureType(paramsEncoder.AddParameter().Type(), param.Type);
        }
    });

    private StandaloneSignatureHandle EncodeLocals(ImmutableArray<AllocatedLocal> locals)
    {
        // We must not encode 0 locals
        if (locals.Length == 0) return default;
        return this.MetadataBuilder.AddStandaloneSignature(this.EncodeBlob(e =>
        {
            var localsEncoder = e.LocalVariableSignature(locals.Length);
            foreach (var local in locals)
            {
                var typeEncoder = localsEncoder.AddVariable().Type();
                Debug.Assert(local.Operand.Type is not null);
                this.EncodeSignatureType(typeEncoder, local.Operand.Type);
            }
        }));
    }

    public void EncodeReturnType(ReturnTypeEncoder encoder, TypeSymbol type)
    {
        if (SymbolEqualityComparer.Default.Equals(type, IntrinsicSymbols.Unit)) { encoder.Void(); return; }

        this.EncodeSignatureType(encoder.Type(), type);
    }

    public void EncodeSignatureType(SignatureTypeEncoder encoder, TypeSymbol type)
    {
        if (type is TypeVariable typeVar) type = typeVar.Substitution;

        if (SymbolEqualityComparer.Default.Equals(type, IntrinsicSymbols.Bool)) { encoder.Boolean(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, IntrinsicSymbols.Int32)) { encoder.Int32(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, IntrinsicSymbols.Float64)) { encoder.Double(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, IntrinsicSymbols.String)) { encoder.String(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, IntrinsicSymbols.Object)) { encoder.Object(); return; }

        if (type.GenericArguments.Length > 0)
        {
            // Generic instantiation
            Debug.Assert(type.GenericDefinition is not null);
            var genericsEncoder = encoder.GenericInstantiation(
                genericType: this.GetEntityHandle(type.GenericDefinition),
                genericArgumentCount: type.GenericArguments.Length,
                isValueType: type.IsValueType);
            foreach (var arg in type.GenericArguments)
            {
                this.EncodeSignatureType(genericsEncoder.AddArgument(), arg);
            }
            return;
        }

        if (type is MetadataTypeSymbol metadataType)
        {
            var reference = this.GetEntityHandle(metadataType);
            encoder.Type(reference, metadataType.IsValueType);
            return;
        }

        // TODO: Multi-dimensional arrays
        if (type is ArrayTypeSymbol { Rank: 1 } arrayType)
        {
            this.EncodeSignatureType(encoder.SZArray(), arrayType.ElementType);
            return;
        }

        if (type is TypeParameterSymbol typeParam)
        {
            if (typeParam.ContainingSymbol is FunctionSymbol func)
            {
                var index = func.GenericParameters.IndexOf(typeParam);
                Debug.Assert(index != -1);
                encoder.GenericMethodTypeParameter(index);
                return;
            }
            if (typeParam.ContainingSymbol is TypeSymbol containingType)
            {
                var index = containingType.GenericParameters.IndexOf(typeParam);
                Debug.Assert(index != -1);
                encoder.GenericTypeParameter(index);
                return;
            }
        }

        // TODO
        throw new NotImplementedException();
    }

    private void WritePe(Stream peStream)
    {
        var debugDirectoryBuilder = this.PdbCodegen?.EncodeDebugDirectory();
        var peHeaderBuilder = new PEHeaderBuilder(
            imageCharacteristics: Characteristics.Dll | Characteristics.ExecutableImage);
        var peBuilder = new ManagedPEBuilder(
            header: peHeaderBuilder,
            metadataRootBuilder: new(this.MetadataBuilder),
            ilStream: this.ilBuilder,
            entryPoint: this.EntryPointHandle,
            flags: CorFlags.ILOnly,
            // TODO: For deterministic builds
            deterministicIdProvider: null,
            debugDirectoryBuilder: debugDirectoryBuilder);

        var peBlob = new BlobBuilder();
        var contentId = peBuilder.Serialize(peBlob);
        peBlob.WriteContentTo(peStream);
    }
}
