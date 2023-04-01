using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Types;
using Assembly = Draco.Compiler.Internal.OptimizingIr.Model.Assembly;
using Parameter = Draco.Compiler.Internal.OptimizingIr.Model.Parameter;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Generates metadata.
/// </summary>
internal sealed class MetadataCodegen
{
    private readonly record struct CompiledMethod(
        BlobHandle SignatureBlobHandle,
        StandaloneSignatureHandle StandaloneSignatureHandle,
        int ParameterIndex);

    // TODO: Doc
    public static void Generate(Assembly assembly, Stream peStream) =>
        throw new System.NotImplementedException();

    private readonly MetadataBuilder metadataBuilder = new();
    private readonly BlobBuilder ilBuilder = new();
    private readonly Dictionary<Global, FieldDefinitionHandle> globalDefinitionHandles = new();
    private readonly Dictionary<IProcedure, CompiledMethod> procedureInfo = new();
    private int parameterIndexCounter = 1;
    private MethodDefinitionHandle entryPointHandle;

    public FieldDefinitionHandle GetGlobalDefinitionHandle(Global global)
    {
        if (!this.globalDefinitionHandles.TryGetValue(global, out var handle))
        {
            var signature = new BlobBuilder();
            var typeEncoder = new BlobEncoder(signature)
                .Field()
                .Type();
            EncodeSignatureType(typeEncoder, global.Type);
            handle = this.metadataBuilder.AddFieldDefinition(
                attributes: FieldAttributes.Public | FieldAttributes.Static,
                name: this.metadataBuilder.GetOrAddString(global.Name),
                signature: this.metadataBuilder.GetOrAddBlob(signature));
            this.globalDefinitionHandles.Add(global, handle);
        }
        return handle;
    }

    public StandaloneSignatureHandle GetProcedureSignatureHandle(IProcedure procedure) =>
        this.GetProcedureInfo(procedure).StandaloneSignatureHandle;

    public UserStringHandle GetStringLiteralHandle(string text) => this.metadataBuilder.GetOrAddUserString(text);

    private CompiledMethod GetProcedureInfo(IProcedure procedure)
    {
        if (!this.procedureInfo.TryGetValue(procedure, out var info))
        {
            var signature = new BlobBuilder();
            var signatureEncoder = new BlobEncoder(signature).MethodSignature();
            var parameterIndex = this.EncodeProcedureSignature(signatureEncoder, procedure);
            var signatureHandle = this.metadataBuilder.GetOrAddBlob(signature);
            var methodSignature = this.metadataBuilder.AddStandaloneSignature(signatureHandle);
            info = new(
                SignatureBlobHandle: signatureHandle,
                StandaloneSignatureHandle: methodSignature,
                ParameterIndex: parameterIndex);
            this.procedureInfo.Add(procedure, info);
        }
        return info;
    }

    private void EncodeAssembly(IAssembly assembly)
    {
        // Go through globals
        foreach (var global in assembly.Globals.Values) this.GetGlobalDefinitionHandle(global);

        // Go through procedures
        foreach (var procedure in assembly.Procedures.Values)
        {
            // Global initializer will get special treatment
            if (ReferenceEquals(assembly.GlobalInitializer, procedure)) continue;

            // Encode the procedure
            this.EncodeProcedure(procedure, procedure.Name);
        }

        // Compile global initializer too
        this.EncodeProcedure(assembly.GlobalInitializer, specialName: ".cctor");
    }

    private MethodDefinitionHandle EncodeProcedure(IProcedure procedure, string? specialName = null)
    {
        // Encode body
        this.ilBuilder.Align(4);
        var methodBodyStream = new MethodBodyStreamEncoder(this.ilBuilder);
        var methodBodyOffset = this.EncodeProcedureBody(methodBodyStream, procedure);

        // Determine attributes
        var attributes = MethodAttributes.Static | MethodAttributes.HideBySig;
        attributes |= specialName is null
            ? MethodAttributes.Public
            : MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        // Retrieve info
        var info = this.GetProcedureInfo(procedure);
        // Actually add definition
        var definitionHandle = this.metadataBuilder.AddMethodDefinition(
            attributes: attributes,
            implAttributes: MethodImplAttributes.IL,
            name: this.metadataBuilder.GetOrAddString(specialName ?? procedure.Name),
            signature: info.SignatureBlobHandle,
            bodyOffset: methodBodyOffset,
            parameterList: MetadataTokens.ParameterHandle(info.ParameterIndex));

        return definitionHandle;
    }

    private int EncodeProcedureSignature(MethodSignatureEncoder encoder, IProcedure procedure)
    {
        // Save index offset
        var parameterIndex = this.parameterIndexCounter;
        this.parameterIndexCounter += procedure.Parameters.Count;
        // Actually encode
        encoder.Parameters(procedure.Parameters.Count, out var returnTypeEncoder, out var parametersEncoder);
        EncodeReturnType(returnTypeEncoder, procedure.ReturnType);
        var paramIndex = 0;
        foreach (var param in procedure.ParametersInDefinitionOrder)
        {
            this.EncodeParameter(parametersEncoder, param, paramIndex);
            ++paramIndex;
        }
        return parameterIndex;
    }

    private void EncodeParameter(ParametersEncoder encoder, Parameter param, int index)
    {
        var parameterTypeEncoder = encoder.AddParameter();
        EncodeSignatureType(parameterTypeEncoder.Type(), param.Type);

        this.metadataBuilder.AddParameter(
            attributes: ParameterAttributes.None,
            name: this.metadataBuilder.GetOrAddString(param.Name),
            sequenceNumber: index + 1);
    }

    private int EncodeProcedureBody(MethodBodyStreamEncoder encoder, IProcedure procedure)
    {
        // TODO: This is where the stackification optimization step could help to reduce local allocation

        // Encode locals, which includes non-stackified registers
        var localsCount = procedure.Locals.Count + procedure.Registers.Count;
        var localsBuilder = new BlobBuilder();
        var localsEncoder = new BlobEncoder(localsBuilder)
            .LocalVariableSignature(localsCount);
        // Actual locals first
        foreach (var local in procedure.LocalsInDefinitionOrder)
        {
            var typeEncoder = localsEncoder
                .AddVariable()
                .Type();
            EncodeSignatureType(typeEncoder, local.Type);
        }
        // Non-stackified registers next
        foreach (var register in procedure.Registers)
        {
            var typeEncoder = localsEncoder
                .AddVariable()
                .Type();
            EncodeSignatureType(typeEncoder, register.Type);
        }

        // Only add the locals if there are more than 0
        var localsHandle = default(StandaloneSignatureHandle);
        if (localsCount > 0)
        {
            localsHandle = this.metadataBuilder.AddStandaloneSignature(this.metadataBuilder.GetOrAddBlob(localsBuilder));
        }

        // We actually need to encode the procedure body now
        var cilEncoder = CilCodegen.GenerateProcedureBody(this, procedure);

        // Actually encode the entire method body
        var methodBodyOffset = encoder.AddMethodBody(
            instructionEncoder: cilEncoder,
            // Since we don't do stackification yet, 8 is fine
            maxStack: 8,
            localVariablesSignature: localsHandle,
            attributes: MethodBodyAttributes.None,
            hasDynamicStackAllocation: false);

        return methodBodyOffset;
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
        if (ReferenceEquals(type, IntrinsicTypes.String)) { encoder.String(); return; }

        // TODO
        throw new System.NotImplementedException();
    }
}
