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

        // TODO
        throw new NotImplementedException();
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

        public void Add(T item)
        {
            this.index.Add(item, this.NextRowId);
            this.rows.Add(item);
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

    private readonly DefinitionIndexWithMarker<Value.Parameter, IReadOnlyProcedure> parameterIndex = new();

    private CilCodegen(IReadOnlyAssembly assembly)
    {
        this.assembly = assembly;
    }

    private void TranslateProcedure(
        MetadataBuilder metadataBuilder,
        BlobBuilder ilBuilder,
        IReadOnlyProcedure procedure)
    {
        var signature = new BlobBuilder();
        var signatureEncoder = new BlobEncoder(signature).MethodSignature();
        this.TranslateProcedureSignature(signatureEncoder, procedure);

        var methodBodyStream = new MethodBodyStreamEncoder(ilBuilder);
        var methodBodyOffset = this.TranslateProcedureBody(methodBodyStream, procedure);
        metadataBuilder.AddMethodDefinition(
            attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            implAttributes: MethodImplAttributes.IL,
            name: metadataBuilder.GetOrAddString(procedure.Name),
            signature: metadataBuilder.GetOrAddBlob(signature),
            bodyOffset: methodBodyOffset,
            parameterList: MetadataTokens.ParameterHandle(this.parameterIndex.GetMark(procedure)));
    }

    private int TranslateProcedureBody(MethodBodyStreamEncoder encoder, IReadOnlyProcedure procedure)
    {
        var codeBuilder = new BlobBuilder();
        var controlFlowBuilder = new ControlFlowBuilder();
        var ilEncoder = new InstructionEncoder(codeBuilder, controlFlowBuilder);

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
        encoder.Parameters(procedure.Parameters.Count, out var returnTypeEncoder, out var parametersEncoder);
        this.TranslateReturnType(returnTypeEncoder, procedure.ReturnType);
        foreach (var param in procedure.Parameters) this.TranslateParameter(parametersEncoder, param);
    }

    private void TranslateParameter(ParametersEncoder encoder, Value.Parameter param)
    {
        this.parameterIndex.Index.Add(param);

        var typeEncoder = encoder.AddParameter();
        this.TranslateSignatureType(typeEncoder.Type(), param.Type);
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
        // TODO
    }

    private void TranslateInstruction(InstructionEncoder encoder, IReadOnlyInstruction instruction)
    {
        // TODO
    }
}
