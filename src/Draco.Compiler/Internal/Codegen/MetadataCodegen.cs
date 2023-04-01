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
    // TODO: Doc
    public static void Generate(Assembly assembly, Stream peStream) =>
        throw new System.NotImplementedException();

    private readonly MetadataBuilder metadataBuilder;

    public EntityHandle GetGlobalHandle(Global global) => throw new System.NotImplementedException();
    public EntityHandle GetProcedureHandle(IProcedure procedure) => throw new System.NotImplementedException();
    public UserStringHandle GetStringLiteralHandle(string text) => throw new System.NotImplementedException();

    private void EncodeProcedureSignature(MethodSignatureEncoder encoder, IProcedure procedure)
    {
        encoder.Parameters(procedure.Parameters.Count, out var returnTypeEncoder, out var parametersEncoder);
        EncodeReturnType(returnTypeEncoder, procedure.ReturnType);
        var paramIndex = 0;
        foreach (var param in procedure.ParametersInDefinitionOrder)
        {
            this.EncodeParameter(parametersEncoder, param, paramIndex);
            ++paramIndex;
        }
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

    private void EncodeProcedureBody(MethodBodyStreamEncoder encoder, IProcedure procedure)
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
