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
        public int NextRowId => this.rows.Count + this.offset;

        public int this[T value] => this.index[value];
        public T this[int index] => this.rows[index - this.offset];

        private readonly int offset;
        private readonly Dictionary<T, int> index = new(ReferenceEqualityComparer.Instance);
        private readonly List<T> rows = new();

        public DefinitionIndex(int offset)
        {
            this.offset = offset;
        }

        public int Add(T item)
        {
            var id = this.NextRowId;
            this.index.Add(item, id);
            this.rows.Add(item);
            return id;
        }

        public void Clear()
        {
            this.index.Clear();
            this.rows.Clear();
        }
    }

    private readonly struct DefinitionIndexWithMarker<TIndexed, TMarker>
        where TIndexed : class
        where TMarker : class
    {
        public DefinitionIndex<TIndexed> Index => this.index;

        public int this[TIndexed value] => this.index[value];
        public int this[TMarker value] => this.markers[value];
        public TIndexed this[int index] => this.index[index];

        private readonly DefinitionIndex<TIndexed> index;
        private readonly Dictionary<TMarker, int> markers = new(ReferenceEqualityComparer.Instance);

        public DefinitionIndexWithMarker(int offset)
        {
            this.index = new(offset);
        }

        public void Clear()
        {
            this.index.Clear();
            this.markers.Clear();
        }

        public int Add(TIndexed item) => this.index.Add(item);
        public void PutMarker(TMarker marker) => this.markers.Add(marker, this.index.NextRowId);
        public int GetMarker(TMarker marker) => this.markers[marker];
    }

    private readonly IReadOnlyAssembly assembly;

    private readonly MetadataBuilder metadataBuilder = new();
    private readonly BlobBuilder ilBuilder = new();

    private readonly Dictionary<IReadOnlyProcedure, BlobHandle> procedureSignatures = new();
    private readonly Dictionary<Global, EntityHandle> globalHandles = new();
    private readonly DefinitionIndexWithMarker<DracoIr.Parameter, IReadOnlyProcedure> parameterIndex = new(offset: 1);
    private readonly DefinitionIndex<object> localIndex = new(offset: 0);
    private readonly Dictionary<IReadOnlyBasicBlock, LabelHandle> labels = new();

    private MethodDefinitionHandle entryPointHandle = default;

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
        // Forward-declare globals
        foreach (var glob in this.assembly.Globals)
        {
            var signature = new BlobBuilder();
            var typeEncoder = new BlobEncoder(signature)
                .Field()
                .Type();
            this.TranslateSignatureType(typeEncoder, glob.Type);
            var handle = this.metadataBuilder.AddFieldDefinition(
                attributes: FieldAttributes.Public | FieldAttributes.Static,
                name: this.metadataBuilder.GetOrAddString(glob.Name),
                signature: this.metadataBuilder.GetOrAddBlob(signature));
            this.globalHandles.Add(glob, handle);
        }
        // Forward-declare signatures
        foreach (var proc in this.assembly.Procedures)
        {
            var signature = new BlobBuilder();
            var signatureEncoder = new BlobEncoder(signature).MethodSignature();
            this.TranslateProcedureSignature(signatureEncoder, proc);
            var signatureHandle = this.metadataBuilder.GetOrAddBlob(signature);
            this.procedureSignatures.Add(proc, signatureHandle);
        }
        // Compile static initializer
        {
            // Header
            var signature = new BlobBuilder();
            var signatureEncoder = new BlobEncoder(signature).MethodSignature();
            this.TranslateProcedureSignature(signatureEncoder, this.assembly.GlobalInitializer);
            var signatureHandle = this.metadataBuilder.GetOrAddBlob(signature);
            this.procedureSignatures.Add(this.assembly.GlobalInitializer, signatureHandle);
            // Body
            this.TranslateProcedure(this.assembly.GlobalInitializer, specialName: ".cctor");
        }
        // Compile procedures
        foreach (var proc in this.assembly.Procedures) this.TranslateProcedure(proc);
    }

    private void TranslateProcedure(IReadOnlyProcedure procedure, string? specialName = null)
    {
        this.ilBuilder.Align(4);
        var methodBodyStream = new MethodBodyStreamEncoder(this.ilBuilder);
        var methodBodyOffset = this.TranslateProcedureBody(methodBodyStream, procedure);

        var parametersStart = this.parameterIndex.GetMarker(procedure);
        var attributes = MethodAttributes.Static | MethodAttributes.HideBySig;
        if (specialName is null)
        {
            attributes |= MethodAttributes.Public;
        }
        else
        {
            attributes |= MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
        }
        var definition = this.metadataBuilder.AddMethodDefinition(
            attributes: attributes,
            implAttributes: MethodImplAttributes.IL,
            name: this.metadataBuilder.GetOrAddString(specialName ?? procedure.Name),
            signature: this.procedureSignatures[procedure],
            bodyOffset: methodBodyOffset,
            parameterList: MetadataTokens.ParameterHandle(parametersStart));

        if (ReferenceEquals(procedure, this.assembly.EntryPoint)) this.entryPointHandle = definition;
    }

    private int TranslateProcedureBody(MethodBodyStreamEncoder encoder, IReadOnlyProcedure procedure)
    {
        var codeBuilder = new BlobBuilder();
        var controlFlowBuilder = new ControlFlowBuilder();
        var ilEncoder = new InstructionEncoder(codeBuilder, controlFlowBuilder);

        // Forward-declare the labels of blocks
        foreach (var bb in procedure.BasicBlocks) this.labels.Add(bb, ilEncoder.DefineLabel());

        // TODO: This is where the stackification optimization step could help to reduce local allocation
        // Pre-declare all locals necessary
        this.localIndex.Clear();
        // Calculate how many instructions will allocate
        // It's the sum of actual local variables and the procedures that do produce something
        var localsCount = procedure.Locals.Count
                        + procedure.Instructions.Count(i => i.Target is not null);
        // Actually encode
        var localsBuilder = new BlobBuilder();
        var localsEncoder = new BlobEncoder(localsBuilder)
            .LocalVariableSignature(localsCount);
        foreach (var local in procedure.Locals)
        {
            var typeEncoder = localsEncoder
                .AddVariable()
                .Type();
            this.TranslateSignatureType(typeEncoder, local.Type);
            this.localIndex.Add(local);
        }
        foreach (var instr in procedure.Instructions)
        {
            if (instr.Target is null) continue;
            var typeEncoder = localsEncoder
                .AddVariable()
                .Type();
            this.TranslateSignatureType(typeEncoder, instr.Target.Type);
            this.localIndex.Add(instr.Target);
        }
        var localsHandle = this.metadataBuilder.AddStandaloneSignature(this.metadataBuilder.GetOrAddBlob(localsBuilder));

        // Translate instructions per basic-block
        foreach (var bb in procedure.BasicBlocks) this.TranslateBasicBlock(ilEncoder, bb);

        var methodBodyOffset = encoder.AddMethodBody(
            instructionEncoder: ilEncoder,
            maxStack: 8,
            localVariablesSignature: localsCount > 0 ? localsHandle : default,
            attributes: 0,
            hasDynamicStackAllocation: false);
        return methodBodyOffset;
    }

    private void TranslateProcedureSignature(MethodSignatureEncoder encoder, IReadOnlyProcedure procedure)
    {
        this.parameterIndex.PutMarker(procedure);

        encoder.Parameters(procedure.Parameters.Count, out var returnTypeEncoder, out var parametersEncoder);
        this.TranslateReturnType(returnTypeEncoder, procedure.ReturnType);
        var paramIndex = 0;
        foreach (var param in procedure.Parameters) this.TranslateParameter(parametersEncoder, param, paramIndex++);
    }

    private void TranslateParameter(ParametersEncoder encoder, DracoIr.Parameter param, int paramIndex)
    {
        this.parameterIndex.Index.Add(param);

        var typeEncoder = encoder.AddParameter();
        this.TranslateSignatureType(typeEncoder.Type(), param.Type);

        this.metadataBuilder.AddParameter(
            attributes: ParameterAttributes.None,
            name: this.metadataBuilder.GetOrAddString(param.Name),
            sequenceNumber: paramIndex + 1);
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
        if (type == Type.String) { encoder.String(); return; }

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
        switch (instruction.Kind)
        {
        case InstructionKind.Nop:
        {
            encoder.OpCode(ILOpCode.Nop);
            break;
        }
        case InstructionKind.Load:
        {
            // We just implement it by copying to the target local
            var targetValue = instruction.Target!;
            var toLoad = instruction[0];
            if (toLoad.IsGlobal())
            {
                encoder.OpCode(ILOpCode.Ldsfld);
                encoder.Token(this.globalHandles[toLoad.AsGlobal()]);
            }
            else
            {
                encoder.LoadLocal(this.localIndex[toLoad]);
            }
            encoder.StoreLocal(this.localIndex[targetValue]);
            break;
        }
        case InstructionKind.Store:
        {
            var target = instruction[0];
            var toStore = instruction[1].AsValue();
            this.TranslateValuePush(encoder, toStore);
            if (target.IsGlobal())
            {
                encoder.OpCode(ILOpCode.Stsfld);
                encoder.Token(this.globalHandles[target.AsGlobal()]);
            }
            else
            {
                encoder.StoreLocal(this.localIndex[target]);
            }
            break;
        }
        case InstructionKind.Jmp:
        {
            var target = instruction[0].AsBlock();
            encoder.Branch(ILOpCode.Br, this.labels[target]);
            break;
        }
        case InstructionKind.JmpIf:
        {
            // push condition
            // brtrue truthy_branch
            // br falsy_branch
            var condition = instruction[0].AsValue();
            var thenBranch = instruction[1].AsBlock();
            var elseBranch = instruction[2].AsBlock();
            this.TranslateValuePush(encoder, condition);
            encoder.Branch(ILOpCode.Brtrue, this.labels[thenBranch]);
            encoder.Branch(ILOpCode.Br, this.labels[elseBranch]);
            break;
        }
        case InstructionKind.Ret:
        {
            var returnedValue = instruction[0].AsValue();
            this.TranslateValuePush(encoder, returnedValue);
            encoder.OpCode(ILOpCode.Ret);
            break;
        }
        case InstructionKind.Neg:
        {
            var targetValue = instruction.Target!;
            var a = instruction[0].AsValue();
            this.TranslateValuePush(encoder, a);
            encoder.OpCode(ILOpCode.Neg);
            encoder.StoreLocal(this.localIndex[targetValue]);
            break;
        }
        case InstructionKind.Add:
        case InstructionKind.Sub:
        case InstructionKind.Mul:
        case InstructionKind.Div:
        case InstructionKind.Rem:
        case InstructionKind.Less:
        case InstructionKind.Equal:
        {
            var targetValue = instruction.Target!;
            var a = instruction[0].AsValue();
            var b = instruction[1].AsValue();
            this.TranslateValuePush(encoder, a);
            this.TranslateValuePush(encoder, b);
            encoder.OpCode(instruction.Kind switch
            {
                InstructionKind.Add => ILOpCode.Add,
                InstructionKind.Sub => ILOpCode.Sub,
                InstructionKind.Mul => ILOpCode.Mul,
                InstructionKind.Div => ILOpCode.Div,
                InstructionKind.Rem => ILOpCode.Rem,
                InstructionKind.Less => ILOpCode.Clt,
                InstructionKind.Equal => ILOpCode.Ceq,
                _ => throw new InvalidOperationException(),
            });
            encoder.StoreLocal(this.localIndex[targetValue]);
            break;
        }
        case InstructionKind.Call:
        {
            var targetValue = instruction.Target;
            var called = instruction[0].AsValue();
            var args = instruction[1].AsArgumentList();
            // Arguments
            foreach (var arg in args.Values) this.TranslateValuePush(encoder, arg);
            // Call
            if (called is Value.Proc procValue)
            {
                var freeFunctionsType = this.metadataBuilder.AddTypeReference(
                    resolutionScope: default,
                    @namespace: default,
                    name: this.metadataBuilder.GetOrAddString("FreeFunctions"));
                var method = this.metadataBuilder.AddMemberReference(
                    parent: freeFunctionsType,
                    name: this.metadataBuilder.GetOrAddString(procValue.Procedure.Name),
                    signature: this.procedureSignatures[procValue.Procedure]);
                encoder.Call(method);
            }
            else
            {
                // TODO
                throw new NotImplementedException();
            }
            // Result
            if (targetValue is not null) encoder.StoreLocal(this.localIndex[targetValue]);
            break;
        }
        default:
            throw new ArgumentOutOfRangeException(nameof(instruction));
        }
    }

    private void TranslateValuePush(InstructionEncoder encoder, Value value)
    {
        if (value.Type == Type.Unit) return;

        if (value is Value.Const constant)
        {
            if (constant.Value is int i4) { encoder.LoadConstantI4(i4); return; }
            if (constant.Value is bool b) { encoder.LoadConstantI4(b ? 1 : 0); return; }
            if (constant.Value is string s) { encoder.LoadString(this.metadataBuilder.GetOrAddUserString(s)); return; }
        }
        if (value is Value.Reg reg)
        {
            encoder.LoadLocal(this.localIndex[reg]);
            return;
        }
        if (value is Value.Param param)
        {
            encoder.LoadArgument(this.parameterIndex[param.Parameter] - 1);
            return;
        }

        throw new NotImplementedException();
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
        // Create type definition for the special <Module> type that holds global functions
        this.metadataBuilder.AddTypeDefinition(
            attributes: default,
            @namespace: default,
            name: this.metadataBuilder.GetOrAddString("<Module>"),
            baseType: default,
            fieldList: MetadataTokens.FieldDefinitionHandle(1),
            methodList: MetadataTokens.MethodDefinitionHandle(1));

        var systemRuntimeRef = this.metadataBuilder.AddAssemblyReference(
            name: this.metadataBuilder.GetOrAddString("System.Runtime"),
            // TODO: What version?
            version: new Version(1, 0),
            culture: default,
            // TODO: We only need to think about it when we decide to support strong naming
            // Apparently it's not really present in Core
            publicKeyOrToken: default,
            flags: default,
            hashValue: default);

        var systemObjectTypeRef = this.metadataBuilder.AddTypeReference(
           resolutionScope: systemRuntimeRef,
           @namespace: this.metadataBuilder.GetOrAddString("System"),
           name: this.metadataBuilder.GetOrAddString("Object"));

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
            entryPoint: this.entryPointHandle,
            flags: CorFlags.ILOnly,
            // TODO: For deterministic builds
            deterministicIdProvider: null);

        var peBlob = new BlobBuilder();
        var contentId = peBuilder.Serialize(peBlob);
        peBlob.WriteContentTo(peStream);
    }
}
