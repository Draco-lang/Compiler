using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Generic;
using Draco.Compiler.Internal.Symbols.Metadata;
using Draco.Compiler.Internal.Symbols.Script;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Symbols.Synthetized.Array;
using static System.Reflection.Metadata.BlobBuilder;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Generates metadata.
/// </summary>
internal sealed class MetadataCodegen : MetadataWriter
{
    public static void Generate(
        Compilation compilation,
        IAssembly assembly,
        Stream peStream,
        Stream? pdbStream,
        CodegenFlags flags = CodegenFlags.None)
    {
        if (pdbStream is not null) flags |= CodegenFlags.EmitPdb;

        var codegen = new MetadataCodegen(
            compilation: compilation,
            assembly: assembly,
            flags: flags);
        codegen.EncodeAssembly();
        codegen.WritePe(peStream);

        if (pdbStream is not null) codegen.PdbCodegen!.WritePdb(pdbStream);
    }

    public static byte[] MicrosoftPublicKeyToken { get; } = [0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a];

    /// <summary>
    /// The compilation that started codegen.
    /// </summary>
    public Compilation Compilation { get; }

    /// <summary>
    /// The flags for the codegen.
    /// </summary>
    public CodegenFlags Flags { get; }

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

    /// <summary>
    /// True, if PDBs should be emitted.
    /// </summary>
    public bool EmitPdb => this.Flags.HasFlag(CodegenFlags.EmitPdb);

    /// <summary>
    /// True, if stackification should be attempted.
    /// </summary>
    public bool Stackify => this.Flags.HasFlag(CodegenFlags.Stackify);

    /// <summary>
    /// True, if handles should be redirected to the compile-time root module.
    /// </summary>
    public bool RedirectHandlesToCompileTimeRoot => this.Flags.HasFlag(CodegenFlags.RedirectHandlesToRoot);

    private WellKnownTypes WellKnownTypes => this.Compilation.WellKnownTypes;

    private readonly IAssembly assembly;
    private readonly BlobBuilder ilBuilder = new();
    private readonly AssemblyReferenceHandle systemRuntimeReference;
    private readonly TypeReferenceHandle systemObjectReference;
    private readonly TypeReferenceHandle systemValueTypeReference;

    private MetadataCodegen(Compilation compilation, IAssembly assembly, CodegenFlags flags)
    {
        this.Compilation = compilation;
        this.Flags = flags;
        // We set stackification to true by default
        this.Flags |= CodegenFlags.Stackify;
        this.assembly = assembly;

        if (this.EmitPdb) this.PdbCodegen = new(this);
        this.WriteModuleAndAssemblyDefinition();

        // Reference System.Object from System.Runtime
        this.systemRuntimeReference = this.GetOrAddAssemblyReference(
            name: "System.Runtime",
            version: new(7, 0, 0, 0),
            publicKeyOrToken: MicrosoftPublicKeyToken);

        this.systemObjectReference = this.GetOrAddTypeReference(
          assembly: this.systemRuntimeReference,
          @namespace: "System",
          name: "Object");

        this.systemValueTypeReference = this.GetOrAddTypeReference(
            assembly: this.systemRuntimeReference,
            @namespace: "System",
            name: "ValueType");
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

    public UserStringHandle GetStringLiteralHandle(string text) => this.MetadataBuilder.GetOrAddUserString(text);

    // TODO: This can be cached by symbol to avoid double reference instertion
    public EntityHandle GetEntityHandle(Symbol symbol)
    {
        switch (symbol)
        {
        case MetadataAssemblySymbol assembly:
            return this.AddAssemblyReference(assembly);

        // Metadata types
        case IMetadataSymbol metadataSymbol when metadataSymbol is MetadataStaticClassSymbol or MetadataTypeSymbol:
            Debug.Assert(symbol.ContainingSymbol is not null);
            return this.GetOrAddTypeReference(
                parent: this.GetContainerEntityHandle(symbol.ContainingSymbol),
                @namespace: GetNamespaceForSymbol(symbol),
                name: metadataSymbol.MetadataName);

        // Generic type instance that is NOT an array
        // Arrays are handled by the case below
        case TypeSymbol typeSymbol when typeSymbol.IsGenericInstance && !typeSymbol.IsArrayType:
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

        case SourceClassSymbol sourceClass:
        {
            // TODO: Possibly very incorrect
            return this.GetOrAddTypeReference(
                parent: this.GetEntityHandle(sourceClass.ContainingSymbol),
                @namespace: null,
                name: sourceClass.MetadataName);
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
            Symbol GetContainingSymbol()
            {
                if (func.IsNested)
                {
                    // TODO: This can be fixed by moving this structural responsibility to the IR
                    // We can't have nested functions represented in metadata directly, so we'll climb up the parent chain
                    // To find the first non-function container
                    return func.AncestorChain
                        .Skip(1)
                        .First(s => s is not FunctionSymbol);
                }

                if (func.ContainingSymbol is not TypeSymbol type) return func.ContainingSymbol!;

                if (!type.IsArrayType) return type;
                // NOTE: This hack is present because of Arrays spit out stuff from their base types
                // to take priority
                // Which means we need to correct them here...
                // Search for the function where the generic definitions are the same but the container is not the array
                var functionInBase = type.BaseTypes
                    .Except([type], SymbolEqualityComparer.Default)
                    .SelectMany(b => b.Members)
                    .OfType<FunctionSymbol>()
                    .First(f => SymbolEqualityComparer.Default.Equals(f.GenericDefinition, func.GenericDefinition));
                return functionInBase.ContainingSymbol!;
            }

            var isInGenericInstance = func.ContainingSymbol?.IsGenericInstance ?? false;
            return this.AddMemberReference(
                // TODO: Should a function ever have a null container?
                // Probably not, let's shove them somewhere known once we can make up our minds
                // This is the case for synthetized ctor functions for example
                parent: func.ContainingSymbol is null
                    ? (EntityHandle)this.ModuleDefinitionHandle
                    : this.GetEntityHandle(GetContainingSymbol()),
                name: func.NestedName,
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

        case SourceModuleSymbol:
        case ScriptModuleSymbol:
        {
            var moduleSymbol = this.RedirectHandlesToCompileTimeRoot
                ? this.Compilation.RootModule
                : (ModuleSymbol)symbol;
            var parent = moduleSymbol.AncestorChain
                .Skip(1)
                .FirstOrDefault(s => s is TypeSymbol or ModuleSymbol);

            var resolutionScope = parent is null
                // Root module, we take the module definition containing it
                ? (EntityHandle)this.ModuleDefinitionHandle
                // We take its parent module
                : this.GetEntityHandle(parent);
            var name = string.IsNullOrEmpty(moduleSymbol.Name)
                ? CompilerConstants.DefaultModuleName
                : moduleSymbol.Name;
            return this.GetOrAddTypeReference(
                parent: resolutionScope,
                @namespace: null,
                name: name);
        }

        case FieldSymbol field:
        {
            var parentHandle = this.GetEntityHandle(field.ContainingSymbol ?? throw new InvalidOperationException());
            // NOTE: Hack for field definitions for generics
            if (field.ContainingSymbol is SourceClassSymbol { IsGenericDefinition: true } sourceClass)
            {
                var genericInstance = this.EncodeBlob(e =>
                {
                    var argsEncoder = e.TypeSpecificationSignature().GenericInstantiation(
                        genericType: parentHandle,
                        genericArgumentCount: sourceClass.GenericParameters.Length,
                        isValueType: sourceClass.IsValueType);
                    foreach (var param in sourceClass.GenericParameters)
                    {
                        this.EncodeSignatureType(argsEncoder.AddArgument(), param);
                    }
                });
                parentHandle = this.MetadataBuilder.AddTypeSpecification(genericInstance);
            }
            return this.AddMemberReference(
                parent: parentHandle,
                name: field.Name,
                signature: this.EncodeBlob(e =>
                {
                    var encoder = e.Field();
                    this.EncodeSignatureType(encoder.Type(), field.Type);
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
            name: CompilerConstants.ConstructorName,
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
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(rank, 1);
        return this.MetadataBuilder.AddTypeSpecification(this.EncodeBlob(e =>
        {
            var encoder = e.TypeSpecificationSignature();
            encoder.Array(out var elementTypeEncoder, out var shapeEncoder);
            this.EncodeSignatureType(elementTypeEncoder, elementType);
            shapeEncoder.Shape(rank, [], []);
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

    private static string? GetNamespaceForSymbol(Symbol? symbol) => symbol switch
    {
        // NOTE: For nested classes we don't need a namespace
        TypeSymbol when symbol.ContainingSymbol is TypeSymbol => null,
        TypeSymbol type => GetNamespaceForSymbol(type.ContainingSymbol),
        MetadataNamespaceSymbol ns => ns.FullName,
        _ when symbol?.ContainingSymbol is not null => GetNamespaceForSymbol(symbol.ContainingSymbol),
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
                name: CompilerConstants.ConstructorName,
                signature: this.EncodeBlob(e =>
                {
                    e.MethodSignature().Parameters(1, out var returnType, out var parameters);
                    returnType.Void();
                    parameters.AddParameter().Type().Type(debuggingModes, true);
                }));
            this.AddAttribute(
                target: this.AssemblyDefinitionHandle,
                ctor: debuggableAttributeCtor,
                value: this.GetOrAddBlob([01, 00, 07, 01, 00, 00, 00, 00]));
        }
    }

    private void EncodeModule(IModule module)
    {
        var fieldIndex = 1;
        var procIndex = 1;
        this.EncodeModule(module, parent: null, fieldIndex: ref fieldIndex, procIndex: ref procIndex);
    }

    private void EncodeModule(IModule module, TypeDefinitionHandle? parent, ref int fieldIndex, ref int procIndex)
    {
        var startFieldIndex = fieldIndex;
        var startProcIndex = procIndex;

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

        // Properties
        var firstProperty = null as PropertyDefinitionHandle?;
        var propertyHandleMap = new Dictionary<Symbol, PropertyDefinitionHandle>();
        foreach (var prop in module.Properties)
        {
            var propHandle = this.EncodeProperty(createdModule, prop);
            firstProperty ??= propHandle;
            propertyHandleMap.Add(prop, propHandle);
        }
        if (firstProperty is not null) this.MetadataBuilder.AddPropertyMap(createdModule, firstProperty.Value);

        // Go through global fields
        foreach (var field in module.Fields)
        {
            this.EncodeField(field);
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

            if (procedure.Symbol is IPropertyAccessorSymbol propAccessor)
            {
                // This is an accessor
                var isGetter = propAccessor.Property.Getter == propAccessor;
                this.MetadataBuilder.AddMethodSemantics(
                    association: propertyHandleMap[propAccessor.Property],
                    semantics: isGetter ? MethodSemanticsAttributes.Getter : MethodSemanticsAttributes.Setter,
                    methodDefinition: handle);
            }
        }

        // Compile global initializer too
        this.EncodeProcedure(module.GlobalInitializer, specialName: ".cctor");
        ++procIndex;

        // We encode every class
        foreach (var @class in module.Classes.Values)
        {
            this.EncodeClass(@class, parent: createdModule, fieldIndex: ref fieldIndex, procIndex: ref procIndex);
        }

        // We encode every submodule
        foreach (var subModule in module.Submodules.Values)
        {
            this.EncodeModule(subModule, createdModule, fieldIndex: ref fieldIndex, procIndex: ref procIndex);
        }
    }

    private FieldDefinitionHandle EncodeField(FieldSymbol field)
    {
        var attributes = GetFieldVisibility(field.Visibility);
        if (field.IsStatic) attributes |= FieldAttributes.Static;

        // Definition
        return this.AddFieldDefinition(
            attributes: attributes,
            name: field.Name,
            signature: this.EncodeFieldSignature(field));
    }

    private MethodDefinitionHandle EncodeProcedure(IProcedure procedure, string? specialName = null)
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
        if (specialName != null) attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        // Parameters
        var parameterList = this.NextParameterHandle;
        foreach (var param in procedure.Parameters)
        {
            var paramHandle = this.AddParameterDefinition(
                attributes: ParameterAttributes.None,
                name: param.Name,
                index: procedure.GetParameterIndex(param));

            // Add attributes
            foreach (var attribute in param.Attributes) this.EncodeAttribute(attribute, paramHandle);
        }

        // Add definition
        var definitionHandle = this.MetadataBuilder.AddMethodDefinition(
            attributes: attributes,
            implAttributes: MethodImplAttributes.IL,
            // TODO: Maybe expose metadata name directly?
            name: this.GetOrAddString(specialName ?? procedure.Symbol.MetadataName),
            signature: this.EncodeProcedureSignature(procedure),
            bodyOffset: methodBodyOffset,
            parameterList: parameterList);

        // Add attributes
        foreach (var attribute in procedure.Attributes) this.EncodeAttribute(attribute, definitionHandle);

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

    private PropertyDefinitionHandle EncodeProperty(
        TypeDefinitionHandle declaringType,
        PropertySymbol prop) => this.MetadataBuilder.AddProperty(
        attributes: PropertyAttributes.None,
        name: this.GetOrAddString(prop.Name),
        signature: this.EncodeBlob(e =>
        {
            e
                .PropertySignature(isInstanceProperty: !prop.IsStatic)
                .Parameters(0, out var returnType, out _);
            this.EncodeReturnType(returnType, prop.Type);
        }));

    private BlobHandle EncodeFieldSignature(FieldSymbol field) =>
        this.EncodeBlob(e => this.EncodeSignatureType(e.Field().Type(), field.Type));

    private BlobHandle EncodeProcedureSignature(IProcedure procedure) => this.EncodeBlob(e =>
    {
        e
            .MethodSignature(genericParameterCount: procedure.Generics.Count, isInstanceMethod: !procedure.IsStatic)
            .Parameters(procedure.Parameters.Count, out var retEncoder, out var paramsEncoder);
        this.EncodeReturnType(retEncoder, procedure.ReturnType);
        foreach (var param in procedure.Parameters)
        {
            this.EncodeSignatureType(paramsEncoder.AddParameter().Type(), param.Type);
        }
    });

    private CustomAttributeHandle EncodeAttribute(AttributeInstance attribute, EntityHandle parent) =>
        this.MetadataBuilder.AddCustomAttribute(
            parent: parent,
            constructor: this.GetEntityHandle(attribute.Constructor),
            value: this.EncodeAttributeSignature(attribute));

    private BlobHandle EncodeAttributeSignature(AttributeInstance attribute)
    {
        if (attribute.FixedArguments.Length == 0 && attribute.NamedArguments.Count == 0) return default;
        return this.EncodeBlob(e =>
        {
            e.CustomAttributeSignature(out var fixedArgs, out var namedArguments);

            foreach (var arg in attribute.FixedArguments)
            {
                this.EncodeAttributeValue(fixedArgs.AddArgument(), arg);
            }

            if (attribute.NamedArguments.Count > 0)
            {
                // TODO: Named arguments
                throw new NotImplementedException();
            }
        });
    }

    private TypeDefinitionHandle EncodeClass(IClass @class, TypeDefinitionHandle? parent, ref int fieldIndex, ref int procIndex)
    {
        var startFieldIndex = fieldIndex;
        var startProcIndex = procIndex;

        var visibility = GetClassVisibility(@class.Symbol.Visibility, parent is not null);
        var attributes = visibility
                       | TypeAttributes.Class | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit | TypeAttributes.Sealed;
        if (@class.Symbol.IsValueType) attributes |= TypeAttributes.SequentialLayout; // AutoLayout = 0.

        var definitionHandle = this.AddTypeDefinition(
            attributes,
            null,
            // TODO: Maybe expose metadata name instead then instead of regular name?
            @class.Symbol.MetadataName,
            @class.Symbol.IsValueType ? this.systemValueTypeReference : this.systemObjectReference,
            fieldList: MetadataTokens.FieldDefinitionHandle(startFieldIndex),
            methodList: MetadataTokens.MethodDefinitionHandle(startProcIndex));

        // Add generic type parameters
        var genericIndex = 0;
        foreach (var typeParam in @class.Generics)
        {
            this.MetadataBuilder.AddGenericParameter(
                parent: definitionHandle,
                attributes: GenericParameterAttributes.None,
                name: this.GetOrAddString(typeParam.Name),
                index: genericIndex++);
        }

        // Properties
        // TODO: Copypasta
        var firstProperty = null as PropertyDefinitionHandle?;
        var propertyHandleMap = new Dictionary<Symbol, PropertyDefinitionHandle>();
        foreach (var prop in @class.Properties)
        {
            var propHandle = this.EncodeProperty(definitionHandle, prop);
            firstProperty ??= propHandle;
            propertyHandleMap.Add(prop, propHandle);
        }
        if (firstProperty is not null) this.MetadataBuilder.AddPropertyMap(definitionHandle, firstProperty.Value);

        // Procedures
        foreach (var proc in @class.Procedures.Values)
        {
            var specialName = proc.Symbol is DefaultConstructorSymbol
                ? ".ctor"
                : null;
            var handle = this.EncodeProcedure(proc, specialName: specialName);
            ++procIndex;

            // TODO: Copypasta
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
                type: definitionHandle,
                packingSize: 0,
                size: 1);
        }

        // If this isn't top level module, we specify nested relationship
        if (parent is not null) this.MetadataBuilder.AddNestedType(definitionHandle, parent.Value);

        return definitionHandle;
    }

    private IEnumerable<TypeSymbol> ScalarConstantTypes => [
        this.WellKnownTypes.SystemSByte,
        this.WellKnownTypes.SystemInt16,
        this.WellKnownTypes.SystemInt32,
        this.WellKnownTypes.SystemInt64,

        this.WellKnownTypes.SystemByte,
        this.WellKnownTypes.SystemUInt16,
        this.WellKnownTypes.SystemUInt32,
        this.WellKnownTypes.SystemUInt64,

        this.WellKnownTypes.SystemBoolean,
        this.WellKnownTypes.SystemChar,

        this.WellKnownTypes.SystemString];

    private void EncodeAttributeValue(LiteralEncoder encoder, ConstantValue constant)
    {
        var type = constant.Type;

        if (this.ScalarConstantTypes.Contains(type, SymbolEqualityComparer.Default))
        {
            encoder.Scalar().Constant(constant.Value);
            return;
        }

        // TODO
        throw new NotImplementedException();
    }

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
        if (SymbolEqualityComparer.Default.Equals(type, WellKnownTypes.Unit)) { encoder.Void(); return; }

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

        if (SymbolEqualityComparer.Default.Equals(type, this.WellKnownTypes.SystemBoolean)) { encoder.Boolean(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.WellKnownTypes.SystemChar)) { encoder.Char(); return; }

        if (SymbolEqualityComparer.Default.Equals(type, this.WellKnownTypes.SystemSByte)) { encoder.SByte(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.WellKnownTypes.SystemInt16)) { encoder.Int16(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.WellKnownTypes.SystemInt32)) { encoder.Int32(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.WellKnownTypes.SystemInt64)) { encoder.Int64(); return; }

        if (SymbolEqualityComparer.Default.Equals(type, this.WellKnownTypes.SystemByte)) { encoder.Byte(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.WellKnownTypes.SystemUInt16)) { encoder.UInt16(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.WellKnownTypes.SystemUInt32)) { encoder.UInt32(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.WellKnownTypes.SystemUInt64)) { encoder.UInt64(); return; }

        if (SymbolEqualityComparer.Default.Equals(type, this.WellKnownTypes.SystemIntPtr)) { encoder.IntPtr(); return; }

        if (SymbolEqualityComparer.Default.Equals(type, this.WellKnownTypes.SystemSingle)) { encoder.Single(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.WellKnownTypes.SystemDouble)) { encoder.Double(); return; }

        if (SymbolEqualityComparer.Default.Equals(type, this.WellKnownTypes.SystemString)) { encoder.String(); return; }
        if (SymbolEqualityComparer.Default.Equals(type, this.WellKnownTypes.SystemObject)) { encoder.Object(); return; }

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
                    shapeEncoder.Shape(arrayType.Rank, [], []);
                    return;
                }
            }

            // Generic instantiation
            Debug.Assert(type.GenericDefinition is not null);
            var typeRef = this.GetEntityHandle(type.GenericDefinition);
            var genericsEncoder = encoder.GenericInstantiation(
                genericType: typeRef,
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

    private static TypeAttributes GetClassVisibility(Api.Semantics.Visibility visibility, bool isNested) => (visibility, isNested) switch
    {
        (Api.Semantics.Visibility.Public, true) => TypeAttributes.NestedPublic,
        (Api.Semantics.Visibility.Public, false) => TypeAttributes.Public,
        (_, true) => TypeAttributes.NestedAssembly,
        (_, false) => TypeAttributes.NotPublic,
    };

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
