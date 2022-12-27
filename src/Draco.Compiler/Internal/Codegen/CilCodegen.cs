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

    private readonly IReadOnlyAssembly assembly;

    private CilCodegen(IReadOnlyAssembly assembly)
    {
        this.assembly = assembly;
    }

    private void TranslateProcedureSignature(MethodSignatureEncoder encoder, IReadOnlyProcecude procedure)
    {
        encoder.Parameters(procedure.Parameters.Count, out var returnTypeEncoder, out var parametersEncoder);
        this.TranslateReturnType(returnTypeEncoder, procedure.ReturnType);
        foreach (var param in procedure.Parameters) this.TranslateParameter(parametersEncoder, param);
    }

    private void TranslateParameter(ParametersEncoder encoder, Value.Parameter param)
    {
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
}
