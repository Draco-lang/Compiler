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
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Types;
using Type = Draco.Compiler.Internal.Types.Type;

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

    private readonly IAssembly assembly;
    private readonly BlobBuilder ilBuilder = new();
    private readonly Dictionary<Global, MemberReferenceHandle> globalReferenceHandles = new();
    private readonly Dictionary<IProcedure, MemberReferenceHandle> procedureReferenceHandles = new();
    private readonly Dictionary<Symbol, MemberReferenceHandle> intrinsicReferenceHandles = new();
    private readonly TypeReferenceHandle freeFunctionsTypeReferenceHandle;

    private MetadataCodegen(Compilation compilation, IAssembly assembly, bool writePdb)
    {
        this.Compilation = compilation;
        if (writePdb) this.PdbCodegen = new(this);
        this.assembly = assembly;
        this.freeFunctionsTypeReferenceHandle = this.GetOrAddTypeReference(
            module: this.ModuleDefinitionHandle,
            @namespace: null,
            name: "FreeFunctions");
        this.LoadIntrinsics();
        this.WriteModuleAndAssemblyDefinition();
    }

    private void LoadIntrinsics()
    {
        var systemConsoleAssembly = this.GetOrAddAssemblyReference(
            name: "System.Console",
            version: new(1, 0));
        var systemConsole = this.GetOrAddTypeReference(
            assembly: systemConsoleAssembly,
            @namespace: "System",
            name: "Console");

        MemberReferenceHandle LoadPrintFunction(string name, System.Action<SignatureTypeEncoder> paramTypeEncoder)
        {
            var signature = this.EncodeBlob(e =>
            {
                e.MethodSignature().Parameters(1, out var retEncoder, out var paramsEncoder);
                retEncoder.Void();
                paramTypeEncoder(paramsEncoder.AddParameter().Type());
            });
            return this.AddMemberReference(
                type: systemConsole,
                name: name,
                signature: signature);
        }

        this.intrinsicReferenceHandles.Add(IntrinsicSymbols.Print_String, LoadPrintFunction("Write", p => p.String()));
        this.intrinsicReferenceHandles.Add(IntrinsicSymbols.Print_Int32, LoadPrintFunction("Write", p => p.Int32()));
        this.intrinsicReferenceHandles.Add(IntrinsicSymbols.Println_String, LoadPrintFunction("WriteLine", p => p.String()));
        this.intrinsicReferenceHandles.Add(IntrinsicSymbols.Println_Int32, LoadPrintFunction("WriteLine", p => p.Int32()));
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
                type: this.freeFunctionsTypeReferenceHandle,
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
                type: this.freeFunctionsTypeReferenceHandle,
                name: procedure.Name,
                signature: signature);
            this.procedureReferenceHandles.Add(procedure, handle);
        }
        return handle;
    }

    public UserStringHandle GetStringLiteralHandle(string text) => this.MetadataBuilder.GetOrAddUserString(text);

    public MemberReferenceHandle GetIntrinsicReferenceHandle(Symbol symbol) => this.intrinsicReferenceHandles[symbol];

    private void EncodeAssembly()
    {
        // Go through globals
        foreach (var global in this.assembly.Globals.Values) this.EncodeGlobal(global);

        // Go through procedures
        foreach (var procedure in this.assembly.Procedures.Values)
        {
            // Global initializer will get special treatment
            if (ReferenceEquals(this.assembly.GlobalInitializer, procedure)) continue;

            // Encode the procedure
            var handle = this.EncodeProcedure(procedure);

            // If this is the entry point, save it
            if (ReferenceEquals(this.assembly.EntryPoint, procedure)) this.EntryPointHandle = handle;
        }

        // Compile global initializer too
        this.EncodeProcedure(this.assembly.GlobalInitializer, specialName: ".cctor");

        // Reference System.Object from System.Runtime
        var systemRuntime = this.GetOrAddAssemblyReference(
            name: "System.Runtime",
            version: new System.Version(7, 0, 0, 0),
            publicKeyOrToken: MicrosoftPublicKeyToken);
        var systemObject = this.GetOrAddTypeReference(
           assembly: systemRuntime,
           @namespace: "System",
           name: "Object");

        // Create the free-functions type
        this.AddTypeDefinition(
            attributes: TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit | TypeAttributes.Abstract | TypeAttributes.Sealed,
            @namespace: default,
            name: "FreeFunctions",
            baseType: systemObject,
            // TODO: Again, this should be read up from an index
            fieldList: MetadataTokens.FieldDefinitionHandle(1),
            // TODO: This depends on the order of types
            // we likely want to read this up from an index
            methodList: MetadataTokens.MethodDefinitionHandle(1));

        // If we write a PDB, we add the debuggable attribute to the assembly
        if (this.PdbCodegen is not null)
        {
            var debuggableAttribute = this.GetOrAddTypeReference(
                assembly: systemRuntime,
                @namespace: "System.Diagnostics",
                name: "DebuggableAttribute");
            var debuggingModes = this.GetOrAddTypeReference(
                containingType: debuggableAttribute,
                @namespace: "System.Diagnostics",
                name: "DebuggingModes");
            var debuggableAttributeCtor = this.AddMemberReference(
                type: debuggableAttribute,
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

    private FieldDefinitionHandle EncodeGlobal(Global global)
    {
        // Definition
        return this.AddFieldDefinition(
            attributes: FieldAttributes.Public | FieldAttributes.Static,
            name: global.Name,
            signature: this.EncodeGlobalSignature(global));
    }

    private MethodDefinitionHandle EncodeProcedure(IProcedure procedure, string? specialName = null)
    {
        var cilCodegen = new CilCodegen(this, procedure);

        // TODO: This is where the stackification optimization step could help to reduce local allocation
        // Encode procedure body
        cilCodegen.EncodeProcedure();

        // Encode locals
        var allocatedLocals = cilCodegen.AllocatedLocals.ToImmutableArray();
        var localsHandle = this.EncodeLocals(allocatedLocals);

        // Encode body
        this.ilBuilder.Align(4);
        var encoder = new MethodBodyStreamEncoder(this.ilBuilder);

        // Actually encode the entire method body
        var methodBodyOffset = encoder.AddMethodBody(
            instructionEncoder: cilCodegen.InstructionEncoder,
            // Since we don't do stackification yet, 8 is fine
            maxStack: 8,
            localVariablesSignature: localsHandle,
            attributes: MethodBodyAttributes.None,
            hasDynamicStackAllocation: false);

        // Determine attributes
        var attributes = MethodAttributes.Static | MethodAttributes.HideBySig;
        attributes |= specialName is null
            ? MethodAttributes.Public
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

        // Finalize
        this.PdbCodegen?.EncodeProcedureDebugInfo(procedure, definitionHandle);

        return definitionHandle;
    }

    private BlobHandle EncodeGlobalSignature(Global global) =>
        this.EncodeBlob(e => EncodeSignatureType(e.Field().Type(), global.Type));

    private BlobHandle EncodeProcedureSignature(IProcedure procedure) => this.EncodeBlob(e =>
    {
        e.MethodSignature().Parameters(procedure.Parameters.Count, out var retEncoder, out var paramsEncoder);
        EncodeReturnType(retEncoder, procedure.ReturnType);
        foreach (var param in procedure.ParametersInDefinitionOrder)
        {
            EncodeSignatureType(paramsEncoder.AddParameter().Type(), param.Type);
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
                EncodeSignatureType(typeEncoder, local.Operand.Type);
            }
        }));
    }

    private static void EncodeReturnType(ReturnTypeEncoder encoder, Type type)
    {
        if (ReferenceEquals(type, IntrinsicTypes.Unit)) { encoder.Void(); return; }

        EncodeSignatureType(encoder.Type(), type);
    }

    private static void EncodeSignatureType(SignatureTypeEncoder encoder, Type type)
    {
        if (ReferenceEquals(type, IntrinsicTypes.Bool)) { encoder.Boolean(); return; }
        if (ReferenceEquals(type, IntrinsicTypes.Int32)) { encoder.Int32(); return; }
        if (ReferenceEquals(type, IntrinsicTypes.Float64)) { encoder.Double(); return; }
        if (ReferenceEquals(type, IntrinsicTypes.String)) { encoder.String(); return; }

        // TODO
        throw new System.NotImplementedException();
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
