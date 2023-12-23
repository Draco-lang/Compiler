using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Generic;
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
    private IntrinsicSymbols IntrinsicSymbols => this.Compilation.IntrinsicSymbols;

    private readonly IAssembly assembly;
    private readonly BlobBuilder ilBuilder = new();
    private readonly Dictionary<IModule, TypeReferenceHandle> moduleReferenceHandles = new();
    private readonly Dictionary<Symbol, MemberReferenceHandle> intrinsicReferenceHandles = new();
    private readonly AssemblyReferenceHandle systemRuntimeReference;
    private readonly TypeReferenceHandle systemObjectReference;

    private MetadataCodegen(Compilation compilation, IAssembly assembly, bool writePdb)
    {
        this.Compilation = compilation;
        if (writePdb) this.PdbCodegen = new(this);
        this.assembly = assembly;
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
        case MetadataAssemblySymbol assembly:
            return this.AddAssemblyReference(assembly);

        // Metadata types
        case IMetadataClass metadataSymbol:
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

        case TypeSymbol typeSymbol:
        {
            var blob = this.EncodeBlob(e =>
            {
                var encoder = e.TypeSpecificationSignature();
                this.EncodeSignatureType(encoder, typeSymbol);
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
                parent: func.ContainingSymbol is null
                    ? this.GetModuleReferenceHandle(this.assembly.RootModule)
                    : this.GetEntityHandle(func.ContainingSymbol),
                name: func.Name,
                signature: this.EncodeBlob(e =>
                {
                    // In generic instances we still need to reference the generic types
                    if (isInGenericInstance) func = func.GenericDefinition!;
                    e
                        .MethodSignature(
                            genericParameterCount: func.GenericParameters.Length,
                            isInstanceMethod: !func.IsStatic)
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

        case FieldSymbol field:
        {
            return this.AddMemberReference(
                parent: this.GetEntityHandle(field.ContainingSymbol
                                          ?? throw new InvalidOperationException()),
                name: field.Name,
                signature: this.EncodeBlob(e =>
                {
                    var encoder = e.Field();
                    this.EncodeSignatureType(encoder.Type(), field.Type);
                }));
        }

        case GlobalSymbol global:
        {
            return this.AddMemberReference(
                parent: this.GetEntityHandle(global.ContainingSymbol
                                          ?? throw new InvalidOperationException()),
                name: global.Name,
                signature: this.EncodeBlob(e =>
                {
                    var encoder = e.Field();
                    this.EncodeSignatureType(encoder.Type(), global.Type);
                }));
        }

        default:
            throw new ArgumentOutOfRangeException(nameof(symbol));
        }
    }

    // TODO: This can be cached
    public EntityHandle GetMultidimensionalArrayCtorHandle(TypeSymbol elementType, int rank) =>
        this.AddMemberReference(
            parent: this.GetMultidimensionalArrayTypeHandle(elementType, rank),
            name: ".ctor",
            signature: this.EncodeBlob(e =>
            {
                e
                    .MethodSignature(isInstanceMethod: true)
                    .Parameters(rank, out var returnTypeEncoder, out var parametersEncoder);
                returnTypeEncoder.Void();
                for (var i = 0; i < rank; ++i) parametersEncoder.AddParameter().Type().Int32();
            }));

    // TODO: This can be cached
    public EntityHandle GetMultidimensionalArrayGetHandle(TypeSymbol elementType, int rank) =>
        this.AddMemberReference(
            parent: this.GetMultidimensionalArrayTypeHandle(elementType, rank),
            name: "Get",
            signature: this.EncodeBlob(e =>
            {
                e
                    .MethodSignature(isInstanceMethod: true)
                    .Parameters(rank, out var returnTypeEncoder, out var parametersEncoder);
                this.EncodeSignatureType(returnTypeEncoder.Type(), elementType);
                for (var i = 0; i < rank; ++i) parametersEncoder.AddParameter().Type().Int32();
            }));

    // TODO: This can be cached
    public EntityHandle GetMultidimensionalArraySetHandle(TypeSymbol elementType, int rank) =>
        this.AddMemberReference(
            parent: this.GetMultidimensionalArrayTypeHandle(elementType, rank),
            name: "Set",
            signature: this.EncodeBlob(e =>
            {
                e
                    .MethodSignature(isInstanceMethod: true)
                    .Parameters(rank + 1, out var returnTypeEncoder, out var parametersEncoder);
                returnTypeEncoder.Void();
                for (var i = 0; i < rank; ++i) parametersEncoder.AddParameter().Type().Int32();
                this.EncodeSignatureType(parametersEncoder.AddParameter().Type(), elementType);
            }));

    // TODO: This can be cached
    private EntityHandle GetMultidimensionalArrayTypeHandle(TypeSymbol elementType, int rank)
    {
        if (rank <= 1) throw new ArgumentOutOfRangeException(nameof(rank));
        return this.MetadataBuilder.AddTypeSpecification(this.EncodeBlob(e =>
        {
            var encoder = e.TypeSpecificationSignature();
            encoder.Array(out var elementTypeEncoder, out var shapeEncoder);
            this.EncodeSignatureType(elementTypeEncoder, elementType);
            shapeEncoder.Shape(rank, ImmutableArray<int>.Empty, ImmutableArray<int>.Empty);
        }));
    }

    private EntityHandle GetContainerEntityHandle(Symbol symbol) => symbol switch
    {
        MetadataNamespaceSymbol ns => this.GetContainerEntityHandle(ns.ContainingSymbol),
        MetadataAssemblySymbol module => this.AddAssemblyReference(module),
        MetadataTypeSymbol type => this.GetOrAddTypeReference(
            assembly: this.AddAssemblyReference(type.Assembly),
            @namespace: GetNamespaceForSymbol(type),
            name: type.MetadataName),
        _ => throw new ArgumentOutOfRangeException(nameof(symbol)),
    };

    private AssemblyReferenceHandle AddAssemblyReference(MetadataAssemblySymbol module) =>
        this.GetOrAddAssemblyReference(
            name: module.Name,
            version: module.Version);

    private static string? GetNamespaceForSymbol(Symbol symbol) => symbol switch
    {
        // NOTE: For nested classes we don't need a namespace
        IMetadataClass mclass when mclass.ContainingSymbol is TypeSymbol => null,
        IMetadataClass mclass => GetNamespaceForSymbol(mclass.ContainingSymbol),
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

    private void EncodeModule(IModule module)
    {
        var fieldIndex = 1;
        var procIndex = 1;
        this.EncodeModule(module, parent: null, fieldIndex: ref fieldIndex, procIndex: ref procIndex);
    }

    private void EncodeModule(
        IModule module,
        TypeDefinitionHandle? parent,
        ref int fieldIndex,
        ref int procIndex)
    {
        var startFieldIndex = fieldIndex;
        var startProcIndex = procIndex;

        // Go through globals
        foreach (var global in module.Globals)
        {
            this.EncodeGlobal(global);
            ++fieldIndex;
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

            ++procIndex;
        }

        // Compile global initializer too
        this.EncodeProcedure(module.GlobalInitializer);
        ++procIndex;

        var visibility = module.Symbol.Visibility == Api.Semantics.Visibility.Public
            ? (parent is not null ? TypeAttributes.NestedPublic : TypeAttributes.Public)
            : (parent is not null ? TypeAttributes.NestedAssembly : TypeAttributes.NotPublic);
        var attributes = visibility | TypeAttributes.Class | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit | TypeAttributes.Abstract | TypeAttributes.Sealed;

        var name = string.IsNullOrEmpty(module.Name) ? CompilerConstants.DefaultModuleName : module.Name;

        // Create the type
        var createdModule = this.AddTypeDefinition(
            attributes: attributes,
            @namespace: default,
            name: name,
            baseType: this.systemObjectReference,
            fieldList: MetadataTokens.FieldDefinitionHandle(startFieldIndex),
            methodList: MetadataTokens.MethodDefinitionHandle(startProcIndex));

        // If this isn't top level module, we specify nested relationship
        if (parent is not null) this.MetadataBuilder.AddNestedType(createdModule, parent.Value);

        // We encode every class
        foreach (var @class in module.Classes.Values)
        {
            this.EncodeClass(@class, parent: createdModule, fieldIndex: ref fieldIndex, procIndex: ref procIndex);
        }

        // We encode every submodule
        foreach (var subModule in module.Submodules.Values)
        {
            this.EncodeModule(subModule, parent: createdModule, fieldIndex: ref fieldIndex, procIndex: ref procIndex);
        }
    }

    private TypeDefinitionHandle EncodeClass(
        IClass @class,
        TypeDefinitionHandle? parent,
        ref int fieldIndex,
        ref int procIndex)
    {
        var startFieldIndex = fieldIndex;
        var startProcIndex = procIndex;

        // TODO: Go through the rest of the members

        // Build up attributes
        var visibility = @class.Symbol.Visibility == Api.Semantics.Visibility.Public
            ? (parent is not null ? TypeAttributes.NestedPublic : TypeAttributes.Public)
            : (parent is not null ? TypeAttributes.NestedAssembly : TypeAttributes.NotPublic);
        var attributes = visibility | TypeAttributes.Class | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit | TypeAttributes.Sealed;
        if (@class.Symbol.IsValueType) attributes |= TypeAttributes.SequentialLayout;

        // Create the type
        var createdClass = this.AddTypeDefinition(
            attributes: attributes,
            @namespace: default,
            name: @class.Name,
            baseType: @class.Symbol.BaseType is null
                ? this.systemObjectReference
                : (TypeReferenceHandle)this.GetEntityHandle(@class.Symbol.BaseType),
            fieldList: MetadataTokens.FieldDefinitionHandle(startFieldIndex),
            methodList: MetadataTokens.MethodDefinitionHandle(startProcIndex));

        // Properties
        PropertyDefinitionHandle? firstProperty = null;
        var propertyHandleMap = new Dictionary<Symbol, PropertyDefinitionHandle>();
        foreach (var prop in @class.Properties)
        {
            var propHandle = this.EncodeProperty(createdClass, prop);
            firstProperty ??= propHandle;
            propertyHandleMap.Add(prop, propHandle);
        }
        if (firstProperty is not null) this.MetadataBuilder.AddPropertyMap(createdClass, firstProperty.Value);

        // Procedures
        foreach (var proc in @class.Procedures.Values)
        {
            var handle = this.EncodeProcedure(proc);
            ++procIndex;

            if (proc.Symbol is IPropertyAccessorSymbol propAccessor)
            {
                // This is an accessor
                var isGetter = propAccessor.Property.Getter == propAccessor;
                this.MetadataBuilder.AddMethodSemantics(
                    association: propertyHandleMap[propAccessor.Property],
                    semantics: isGetter ? MethodSemanticsAttributes.Getter : MethodSemanticsAttributes.Setter,
                    methodDefinition: handle);
            }
        }

        // Fields
        foreach (var field in @class.Fields)
        {
            this.EncodeField(field);
            ++fieldIndex;
        }

        // If this is a valuetype without fields, we add .pack 0 and .size 1
        if (@class.Symbol.IsValueType && @class.Fields.Count == 0)
        {
            this.MetadataBuilder.AddTypeLayout(
                type: createdClass,
                packingSize: 0,
                size: 1);
        }

        // If this isn't top level module, we specify nested relationship
        if (parent is not null) this.MetadataBuilder.AddNestedType(createdClass, parent.Value);

        return createdClass;
    }

    private FieldDefinitionHandle EncodeGlobal(GlobalSymbol global)
    {
        var visibility = GetFieldVisibility(global.Visibility);

        // Definition
        return this.AddFieldDefinition(
            attributes: visibility | FieldAttributes.Static,
            name: global.Name,
            signature: this.EncodeGlobalSignature(global));
    }

    private FieldDefinitionHandle EncodeField(FieldSymbol field)
    {
        var visibility = GetFieldVisibility(field.Visibility);
        var mutability = field.IsMutable ? default : FieldAttributes.InitOnly;

        // Definition
        return this.AddFieldDefinition(
            attributes: visibility | mutability,
            name: field.Name,
            signature: this.EncodeFieldSignature(field));
    }

    private MethodDefinitionHandle EncodeProcedure(IProcedure procedure)
    {
        var visibility = GetMethodVisibility(procedure.Symbol.Visibility);

        // Encode instructions
        var cilCodegen = new CilCodegen(this, procedure);
        cilCodegen.EncodeProcedure();

        // Encode locals
        var allocatedLocals = cilCodegen.AllocatedLocals.ToImmutableArray();
        var allocatedRegisters = cilCodegen.AllocatedRegisters.ToImmutableArray();
        var localsHandle = this.EncodeLocals(allocatedLocals, allocatedRegisters);

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
        var attributes = MethodAttributes.HideBySig | visibility;
        if (procedure.Symbol.IsStatic) attributes |= MethodAttributes.Static;
        if (procedure.Symbol.IsSpecialName) attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        // Parameters
        var parameterList = this.NextParameterHandle;
        foreach (var param in procedure.Parameters)
        {
            this.AddParameterDefinition(
                attributes: ParameterAttributes.None,
                name: param.Name,
                index: procedure.GetParameterIndex(param));
        }

        // Add definition
        var definitionHandle = this.MetadataBuilder.AddMethodDefinition(
            attributes: attributes,
            implAttributes: MethodImplAttributes.IL,
            name: this.GetOrAddString(procedure.Name),
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

    private PropertyDefinitionHandle EncodeProperty(TypeDefinitionHandle declaringType, PropertySymbol prop)
    {
        return this.MetadataBuilder.AddProperty(
            attributes: PropertyAttributes.None,
            name: this.GetOrAddString(prop.Name),
            signature: this.EncodeBlob(e =>
            {
                e
                    .PropertySignature(isInstanceProperty: !prop.IsStatic)
                    .Parameters(0, out var returnType, out _);
                this.EncodeReturnType(returnType, prop.Type);
            }));
    }

    private BlobHandle EncodeGlobalSignature(GlobalSymbol global) =>
        this.EncodeBlob(e => this.EncodeSignatureType(e.Field().Type(), global.Type));

    private BlobHandle EncodeFieldSignature(FieldSymbol field) =>
        this.EncodeBlob(e => this.EncodeSignatureType(e.Field().Type(), field.Type));

    private BlobHandle EncodeProcedureSignature(IProcedure procedure) => this.EncodeBlob(e =>
    {
        e
            .MethodSignature(
                genericParameterCount: procedure.Generics.Count,
                isInstanceMethod: !procedure.Symbol.IsStatic)
            .Parameters(
                procedure.Parameters.Count,
                out var retEncoder,
                out var paramsEncoder);
        this.EncodeReturnType(retEncoder, procedure.ReturnType);
        foreach (var param in procedure.Parameters)
        {
            this.EncodeSignatureType(paramsEncoder.AddParameter().Type(), param.Type);
        }
    });

    private StandaloneSignatureHandle EncodeLocals(
        ImmutableArray<AllocatedLocal> locals,
        ImmutableArray<Register> registers)
    {
        // We must not encode 0 locals
        if (locals.Length + registers.Length == 0) return default;
        return this.MetadataBuilder.AddStandaloneSignature(this.EncodeBlob(e =>
        {
            var localsEncoder = e.LocalVariableSignature(locals.Length + registers.Length);
            foreach (var local in locals)
            {
                var typeEncoder = localsEncoder.AddVariable().Type();
                this.EncodeSignatureType(typeEncoder, local.Symbol.Type);
            }
            foreach (var register in registers)
            {
                var typeEncoder = localsEncoder.AddVariable().Type();
                this.EncodeSignatureType(typeEncoder, register.Type);
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

        if (type is TypeInstanceSymbol instance && !instance.IsGenericInstance)
        {
            // Unwrap
            this.EncodeSignatureType(encoder, instance.GenericDefinition);
            return;
        }

        if (SymbolEqualityComparer.Default.Equals(type, this.IntrinsicSymbols.Bool)) { encoder.Boolean(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.IntrinsicSymbols.Char)) { encoder.Char(); return; }

        if (SymbolEqualityComparer.Default.Equals(type, this.IntrinsicSymbols.Int8)) { encoder.SByte(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.IntrinsicSymbols.Int16)) { encoder.Int16(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.IntrinsicSymbols.Int32)) { encoder.Int32(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.IntrinsicSymbols.Int64)) { encoder.Int64(); return; }

        if (SymbolEqualityComparer.Default.Equals(type, this.IntrinsicSymbols.Uint8)) { encoder.Byte(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.IntrinsicSymbols.Uint16)) { encoder.UInt16(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.IntrinsicSymbols.Uint32)) { encoder.UInt32(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.IntrinsicSymbols.Uint64)) { encoder.UInt64(); return; }

        if (SymbolEqualityComparer.Default.Equals(type, this.IntrinsicSymbols.Float32)) { encoder.Single(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.IntrinsicSymbols.Float64)) { encoder.Double(); return; }

        if (SymbolEqualityComparer.Default.Equals(type, this.IntrinsicSymbols.String)) { encoder.String(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.IntrinsicSymbols.Object)) { encoder.Object(); return; }

        if (type.GenericArguments.Length > 0)
        {
            // Check, if this is an array
            if (type.GenericDefinition is ArrayTypeSymbol arrayType)
            {
                var elementType = type.GenericArguments[0];
                if (arrayType.Rank == 1)
                {
                    // One-dimensional arrays are special
                    Debug.Assert(type.GenericArguments.Length == 1);
                    this.EncodeSignatureType(encoder.SZArray(), elementType);
                    return;
                }
                {
                    // Multi-dimensional
                    encoder.Array(out var elementTypeEncoder, out var shapeEncoder);
                    this.EncodeSignatureType(elementTypeEncoder, elementType);
                    shapeEncoder.Shape(arrayType.Rank, ImmutableArray<int>.Empty, ImmutableArray<int>.Empty);
                    return;
                }
            }

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

        if (type is ReferenceTypeSymbol referenceType)
        {
            this.EncodeSignatureType(encoder.Pointer(), referenceType.ElementType);
            return;
        }

        if (type is SourceClassSymbol sourceClass)
        {
            encoder.Type(
                type: this.GetOrAddTypeReference(
                    parent: this.GetEntityHandle(sourceClass.ContainingSymbol),
                    @namespace: null,
                    name: sourceClass.MetadataName),
                isValueType: sourceClass.IsValueType);
            return;
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

    private static FieldAttributes GetFieldVisibility(Api.Semantics.Visibility visibility) => visibility switch
    {
        Api.Semantics.Visibility.Public => FieldAttributes.Public,
        Api.Semantics.Visibility.Internal => FieldAttributes.Assembly,
        Api.Semantics.Visibility.Private => FieldAttributes.Private,
        _ => throw new ArgumentOutOfRangeException(nameof(visibility)),
    };

    private static MethodAttributes GetMethodVisibility(Api.Semantics.Visibility visibility) => visibility switch
    {
        Api.Semantics.Visibility.Public => MethodAttributes.Public,
        Api.Semantics.Visibility.Internal => MethodAttributes.Assembly,
        Api.Semantics.Visibility.Private => MethodAttributes.Private,
        _ => throw new ArgumentOutOfRangeException(nameof(visibility)),
    };
}
