using System;
using System.Collections.Generic;
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
using Parameter = Draco.Compiler.Internal.OptimizingIr.Model.Parameter;
using Type = Draco.Compiler.Internal.Types.Type;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Generates metadata.
/// </summary>
internal sealed class MetadataCodegen : MetadataWriterBase
{
    public static void Generate(Compilation compilation, IAssembly assembly, Stream peStream, Stream? pdbStream)
    {
        var codegen = new MetadataCodegen(
            compilation: compilation,
            assembly: assembly,
            writePdb: pdbStream is not null);
        codegen.EncodeAssembly();
        codegen.WritePe(peStream);
        if (pdbStream is not null)
        {
            codegen.PdbCodegen!.WritePdb(pdbStream);
        }
    }

    /// <summary>
    /// The compilation that started codegen.
    /// </summary>
    public Compilation Compilation { get; }

    /// <summary>
    /// The PDB code-generator, in case we generate PDBs.
    /// </summary>
    public PdbCodegen? PdbCodegen { get; }

    /// <summary>
    /// Handle for the entry point.
    /// </summary>
    public MethodDefinitionHandle EntryPointHandle { get; private set; }

    private readonly IAssembly assembly;
    private readonly BlobBuilder ilBuilder = new();
    private readonly Dictionary<Global, MemberReferenceHandle> globalReferenceHandles = new();
    private readonly Dictionary<IProcedure, MemberReferenceHandle> procedureReferenceHandles = new();
    private readonly TypeReferenceHandle freeFunctionsTypeReferenceHandle;
    private readonly Dictionary<Symbol, MemberReferenceHandle> intrinsics = new();
    private int parameterIndexCounter = 1;

    private MetadataCodegen(Compilation compilation, IAssembly assembly, bool writePdb)
        : base(assembly.Name)
    {
        this.Compilation = compilation;
        if (writePdb) this.PdbCodegen = new(this);
        this.assembly = assembly;
        this.freeFunctionsTypeReferenceHandle = this.AddTypeReference(
            module: this.ModuleDefinitionHandle,
            @namespace: null,
            name: "FreeFunctions");
        this.LoadIntrinsics();
    }

    private void LoadIntrinsics()
    {
        var systemConsoleAssembly = this.AddAssemblyReference(
            name: "System.Console",
            version: new(1, 0));
        var systemConsole = this.AddTypeReference(
            assembly: systemConsoleAssembly,
            @namespace: "System",
            name: "Console");

        MemberReferenceHandle LoadPrintFunction(string name, System.Action<SignatureTypeEncoder> paramTypeEncoder)
        {
            var signature = new BlobBuilder();
            new BlobEncoder(signature)
                .MethodSignature()
                .Parameters(1, out var retEncoder, out var paramsEncoder);
            retEncoder.Void();
            paramTypeEncoder(paramsEncoder.AddParameter().Type());
            return this.AddMethodReference(
                type: systemConsole,
                name: name,
                signature: this.GetOrAddBlob(signature));
        }

        this.intrinsics.Add(IntrinsicSymbols.Print_String, LoadPrintFunction("Write", p => p.String()));
        this.intrinsics.Add(IntrinsicSymbols.Print_Int32, LoadPrintFunction("Write", p => p.Int32()));
        this.intrinsics.Add(IntrinsicSymbols.Println_String, LoadPrintFunction("WriteLine", p => p.String()));
        this.intrinsics.Add(IntrinsicSymbols.Println_Int32, LoadPrintFunction("WriteLine", p => p.Int32()));
    }

    public MemberReferenceHandle GetGlobalReferenceHandle(Global global)
    {
        if (!this.globalReferenceHandles.TryGetValue(global, out var handle))
        {
            // Encode signature
            var signature = new BlobBuilder();
            var typeEncoder = new BlobEncoder(signature)
                .Field()
                .Type();
            EncodeSignatureType(typeEncoder, global.Type);
            // Add the field reference
            handle = this.MetadataBuilder.AddMemberReference(
                parent: this.freeFunctionsTypeReferenceHandle,
                name: this.GetOrAddString(global.Name),
                signature: this.GetOrAddBlob(signature));
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
            handle = this.AddMethodReference(
                type: this.freeFunctionsTypeReferenceHandle,
                name: procedure.Name,
                signature: signature);
            this.procedureReferenceHandles.Add(procedure, handle);
        }
        return handle;
    }

    public UserStringHandle GetStringLiteralHandle(string text) => this.MetadataBuilder.GetOrAddUserString(text);

    public MemberReferenceHandle GetIntrinsicHandle(Symbol symbol) => this.intrinsics[symbol];
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
        var systemRuntime = this.AddAssemblyReference(
            name: "System.Runtime",
            version: new System.Version(7, 0, 0, 0),
            publicKeyOrToken: this.MicrosoftPublicKeyToken);
        var systemObject = this.AddTypeReference(
           assembly: systemRuntime,
           @namespace: "System",
           name: "Object");

        // Create the free-functions type
        this.MetadataBuilder.AddTypeDefinition(
            attributes: TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit | TypeAttributes.Abstract | TypeAttributes.Sealed,
            @namespace: default,
            name: this.GetOrAddString("FreeFunctions"),
            baseType: systemObject,
            // TODO: Again, this should be read up from an index
            fieldList: MetadataTokens.FieldDefinitionHandle(1),
            // TODO: This depends on the order of types
            // we likely want to read this up from an index
            methodList: MetadataTokens.MethodDefinitionHandle(1));

        // If we write a PDB, we add the debuggable attribute to the assembly
        if (this.PdbCodegen is not null)
        {
            var debuggableAttribute = this.AddTypeReference(
                assembly: systemRuntime,
                @namespace: "System.Diagnostics",
                name: "DebuggableAttribute");
            var debuggingModes = this.AddTypeReference(
                containingType: debuggableAttribute,
                @namespace: "System.Diagnostics",
                name: "DebuggingModes");
            var debuggableAttributeCtor = this.AddMethodReference(
                type: debuggableAttribute,
                name: ".ctor",
                signature: encoder =>
                {
                    encoder.Parameters(1, out var returnType, out var parameters);
                    returnType.Void();
                    parameters.AddParameter().Type().Type(debuggingModes, true);
                });
            this.MetadataBuilder.AddCustomAttribute(
                parent: this.AssemblyDefinitionHandle,
                constructor: debuggableAttributeCtor,
                value: this.MetadataBuilder.GetOrAddBlob(new byte[] { 01, 00, 07, 01, 00, 00, 00, 00 }));
        }
    }

    private FieldDefinitionHandle EncodeGlobal(Global global)
    {
        // Signature
        var signature = new BlobBuilder();
        var typeEncoder = new BlobEncoder(signature)
            .Field()
            .Type();
        EncodeSignatureType(typeEncoder, global.Type);
        // Definition
        return this.MetadataBuilder.AddFieldDefinition(
            attributes: FieldAttributes.Public | FieldAttributes.Static,
            name: this.GetOrAddString(global.Name),
            signature: this.GetOrAddBlob(signature));
    }

    private MethodDefinitionHandle EncodeProcedure(IProcedure procedure, string? specialName = null)
    {
        // Encode body
        this.ilBuilder.Align(4);
        var encoder = new MethodBodyStreamEncoder(this.ilBuilder);

        var cilCodegen = new CilCodegen(this, procedure);

        // TODO: This is where the stackification optimization step could help to reduce local allocation
        // Encode procedure body
        cilCodegen.EncodeProcedure();

        // Encode local types
        var localTypes = cilCodegen.LocalTypes.ToList();
        var localsBuilder = new BlobBuilder();
        var localsEncoder = new BlobEncoder(localsBuilder)
            .LocalVariableSignature(localTypes.Count);
        foreach (var localType in localTypes)
        {
            var typeEncoder = localsEncoder
                .AddVariable()
                .Type();
            EncodeSignatureType(typeEncoder, localType);
        }

        // Only add the locals if there are more than 0
        var localsHandle = localTypes.Count > 0
            ? this.MetadataBuilder.AddStandaloneSignature(this.GetOrAddBlob(localsBuilder))
            : default;

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

        // Retrieve info
        var signature = this.EncodeProcedureSignature(procedure);
        var parameterIndex = this.parameterIndexCounter;
        this.parameterIndexCounter += procedure.Parameters.Count;
        var paramIndex = 0;
        foreach (var param in procedure.ParametersInDefinitionOrder)
        {
            this.MetadataBuilder.AddParameter(
                attributes: ParameterAttributes.None,
                name: this.GetOrAddString(param.Name),
                sequenceNumber: paramIndex + 1);
            ++paramIndex;
        }

        // Add definition
        var definitionHandle = this.MetadataBuilder.AddMethodDefinition(
            attributes: attributes,
            implAttributes: MethodImplAttributes.IL,
            name: this.GetOrAddString(specialName ?? procedure.Name),
            signature: signature,
            bodyOffset: methodBodyOffset,
            parameterList: MetadataTokens.ParameterHandle(parameterIndex));

        // Finalize
        cilCodegen.FinalizeProcedure(definitionHandle);

        return definitionHandle;
    }

    private BlobHandle EncodeProcedureSignature(IProcedure procedure)
    {
        var blob = new BlobBuilder();
        var encoder = new BlobEncoder(blob);

        encoder.MethodSignature().Parameters(procedure.Parameters.Count, out var retEncoder, out var paramsEncoder);
        EncodeReturnType(retEncoder, procedure.ReturnType);
        foreach (var param in procedure.ParametersInDefinitionOrder)
        {
            EncodeSignatureType(paramsEncoder.AddParameter().Type(), param.Type);
        }

        return this.GetOrAddBlob(blob);
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
        var debugDirectoryBuilder = this.PdbCodegen?.EncodeDebugDirectory(this.assembly);
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
