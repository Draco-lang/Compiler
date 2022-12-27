using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.DracoIr;
using Type = Draco.Compiler.Internal.DracoIr.Type;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Generates CIL from DracoIR.
/// </summary>
internal sealed class CilCodegen
{
    /// <summary>
    /// Generates a PE file from the given Draco assembly.
    /// </summary>
    /// <param name="assembly">The assembly to generate an executable from.</param>
    /// <param name="peStream">The stream to write the executable to.</param>
    public static void Generate(IReadOnlyAssembly assembly, Stream peStream)
    {
        var codegen = new CilCodegen(assembly);
        codegen.Translate();
        codegen.WritePe(peStream);
    }

    private readonly struct DefinitionIndex<T>
        where T : class
    {
        public int NextRowId => this.rows.Count + 1;

        private readonly Dictionary<T, int> index = new(ReferenceEqualityComparer.Instance);
        private readonly List<T> rows = new();

        public DefinitionIndex()
        {
        }

        public int Add(T item)
        {
            var id = this.NextRowId;
            this.index.Add(item, id);
            this.rows.Add(item);
            return id;
        }
    }

    private readonly struct DefinitionIndexWithMarker<TIndexed, TMarker>
        where TIndexed : class
        where TMarker : class
    {
        public DefinitionIndex<TIndexed> Index => this.index;

        private readonly DefinitionIndex<TIndexed> index = new();
        private readonly Dictionary<TMarker, int> markers = new(ReferenceEqualityComparer.Instance);

        public DefinitionIndexWithMarker()
        {
        }

        public void PutMark(TMarker marker) => this.markers.Add(marker, this.index.NextRowId);
        public int GetMark(TMarker marker) => this.markers[marker];
    }

    private readonly IReadOnlyAssembly assembly;

    private readonly MetadataBuilder metadataBuilder = new();
    private readonly BlobBuilder ilBuilder = new();

    private readonly DefinitionIndexWithMarker<Value.Parameter, IReadOnlyProcedure> parameterIndex = new();
    private readonly Dictionary<IReadOnlyBasicBlock, LabelHandle> labels = new();

    private CilCodegen(IReadOnlyAssembly assembly)
    {
        this.assembly = assembly;
    }

    private void Translate()
    {
        this.DefineModuleAndAssembly();
        this.TranslateAssembly();
        this.DefineFreeFunctionsType();
    }

    private void TranslateAssembly()
    {
        foreach (var proc in this.assembly.Procedures.Values) this.TranslateProcedure(proc);
    }

    private void TranslateProcedure(IReadOnlyProcedure procedure)
    {
        var signature = new BlobBuilder();
        var signatureEncoder = new BlobEncoder(signature).MethodSignature();
        this.TranslateProcedureSignature(signatureEncoder, procedure);

        this.ilBuilder.Align(4);
        var methodBodyStream = new MethodBodyStreamEncoder(this.ilBuilder);
        var methodBodyOffset = this.TranslateProcedureBody(methodBodyStream, procedure);

        var parametersStart = this.parameterIndex.GetMark(procedure);
        this.metadataBuilder.AddMethodDefinition(
            attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            implAttributes: MethodImplAttributes.IL,
            name: this.metadataBuilder.GetOrAddString(procedure.Name),
            signature: this.metadataBuilder.GetOrAddBlob(signature),
            bodyOffset: methodBodyOffset,
            parameterList: MetadataTokens.ParameterHandle(parametersStart));
    }

    private int TranslateProcedureBody(MethodBodyStreamEncoder encoder, IReadOnlyProcedure procedure)
    {
        var codeBuilder = new BlobBuilder();
        var controlFlowBuilder = new ControlFlowBuilder();
        var ilEncoder = new InstructionEncoder(codeBuilder, controlFlowBuilder);

        // Forward-declare the labels of blocks
        foreach (var bb in procedure.BasicBlocks) this.labels.Add(bb, ilEncoder.DefineLabel());

        // Translate instructions per basic-block
        foreach (var bb in procedure.BasicBlocks) this.TranslateBasicBlock(ilEncoder, bb);

        var methodBody = encoder.AddMethodBody(
            codeSize: codeBuilder.Count,
            maxStack: 8,
            exceptionRegionCount: 0,
            hasSmallExceptionRegions: false,
            localVariablesSignature: default,
            attributes: 0,
            hasDynamicStackAllocation: false);
        var methodBodyWriter = new BlobWriter(methodBody.Instructions);
        methodBodyWriter.WriteBytes(codeBuilder);

        return methodBody.Offset;
    }

    private void TranslateProcedureSignature(MethodSignatureEncoder encoder, IReadOnlyProcedure procedure)
    {
        this.parameterIndex.PutMark(procedure);

        encoder.Parameters(procedure.Parameters.Count, out var returnTypeEncoder, out var parametersEncoder);
        this.TranslateReturnType(returnTypeEncoder, procedure.ReturnType);
        foreach (var param in procedure.Parameters) this.TranslateParameter(parametersEncoder, param);
    }

    private void TranslateParameter(ParametersEncoder encoder, Value.Parameter param)
    {
        this.parameterIndex.Index.Add(param);

        var typeEncoder = encoder.AddParameter();
        this.TranslateSignatureType(typeEncoder.Type(), param.Type);

        this.metadataBuilder.AddParameter(
            attributes: ParameterAttributes.None,
            name: this.metadataBuilder.GetOrAddString(param.Name),
            sequenceNumber: param.Index + 1);
    }

    private void TranslateReturnType(ReturnTypeEncoder encoder, Type type)
    {
        if (type == Type.Unit) { encoder.Void(); return; }

        this.TranslateSignatureType(encoder.Type(), type);
    }

    private void TranslateSignatureType(SignatureTypeEncoder encoder, Type type)
    {
        if (type == Type.Bool) { encoder.Boolean(); return; }
        if (type == Type.Int32) { encoder.Int32(); return; }

        // TODO
        throw new NotImplementedException();
    }

    private void TranslateBasicBlock(InstructionEncoder encoder, IReadOnlyBasicBlock basicBlock)
    {
        encoder.MarkLabel(this.labels[basicBlock]);
        foreach (var instr in basicBlock.Instructions) this.TranslateInstruction(encoder, instr);
    }

    private void TranslateInstruction(InstructionEncoder encoder, IReadOnlyInstruction instruction)
    {
        // TODO
        encoder.OpCode(ILOpCode.Nop);
    }

    private void DefineModuleAndAssembly()
    {
        this.metadataBuilder.AddModule(
            generation: 0,
            moduleName: this.metadataBuilder.GetOrAddString(this.assembly.Name),
            // TODO: Proper module-version ID
            mvid: this.metadataBuilder.GetOrAddGuid(Guid.NewGuid()),
            // TODO: What are these? Encryption?
            encId: default,
            encBaseId: default);

        this.metadataBuilder.AddAssembly(
            name: this.metadataBuilder.GetOrAddString(this.assembly.Name),
            // TODO: Proper versioning
            version: new Version(1, 0, 0, 0),
            culture: default,
            publicKey: default,
            flags: 0,
            hashAlgorithm: AssemblyHashAlgorithm.None);
    }

    private void DefineFreeFunctionsType()
    {
        // TODO: Replace with System.Runtime reference
        var mscorlibAssemblyRef = this.metadataBuilder.AddAssemblyReference(
            name: this.metadataBuilder.GetOrAddString("mscorlib"),
            // TODO: What version?
            version: new Version(4, 0, 0, 0),
            culture: default,
            // TODO: What the hell?
            publicKeyOrToken: this.metadataBuilder.GetOrAddBlob(new byte[]
            {
                0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89,
            }),
            flags: default,
            hashValue: default);

        var systemObjectTypeRef = this.metadataBuilder.AddTypeReference(
           mscorlibAssemblyRef,
           this.metadataBuilder.GetOrAddString("System"),
           this.metadataBuilder.GetOrAddString("Object"));

        // TODO: Factor out constants into settings?
        this.metadataBuilder.AddTypeDefinition(
            attributes: TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit | TypeAttributes.Abstract | TypeAttributes.Sealed,
            @namespace: default,
            name: this.metadataBuilder.GetOrAddString("FreeFunctions"),
            baseType: systemObjectTypeRef,
            // TODO: Again, this should be read up from an index
            fieldList: MetadataTokens.FieldDefinitionHandle(1),
            // TODO: This depends on the order of types
            // we likely want to read this up from an index
            methodList: MetadataTokens.MethodDefinitionHandle(1));
    }

    private void WritePe(Stream peStream)
    {
        var peHeaderBuilder = new PEHeaderBuilder(
            imageCharacteristics: Characteristics.Dll);
        var peBuilder = new ManagedPEBuilder(
            header: peHeaderBuilder,
            metadataRootBuilder: new(this.metadataBuilder),
            ilStream: this.ilBuilder,
            // TODO: When entry point is exposed from assembly
            entryPoint: default,
            flags: CorFlags.ILOnly,
            // TODO: For deterministic builds
            deterministicIdProvider: null);

        var peBlob = new BlobBuilder();
        var contentId = peBuilder.Serialize(peBlob);
        peBlob.WriteContentTo(peStream);
    }
}
