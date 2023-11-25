using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Metadata;

namespace Draco.Compiler.Tests.Decompilation;

internal static class CilFormatter
{
    public static string VisualizeIl(CompiledLibrary library, MetadataMethodSymbol func, PEReader peReader, MetadataReader reader)
    {
        var body = peReader.GetMethodBody(func.BodyRelativeVirtualAddress);

        var sb = new StringBuilder();
        sb.AppendLine("{");

        // TODO: branching & exception handling
        WriteBodyProlog(library, func, reader, body, sb);
        WriteInstructions(library, func, reader, body, sb);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static unsafe void WriteInstructions(CompiledLibrary library, MetadataMethodSymbol func, MetadataReader reader, MethodBodyBlock body, StringBuilder sb)
    {
        var blobReader = body.GetILReader();

        var span = new ReadOnlySpan<byte>(blobReader.StartPointer, blobReader.Length);

        while (!span.IsEmpty)
        {
            var instruction = InstructionDecoder.Read(span, library.Codegen.GetSymbol, reader.GetUserString, out var advance);
            span = span[advance..];

            sb.Append(InstructionDecoder.GetText(instruction.OpCode));

            switch (instruction.Operand)
            {
            case Symbol symbol:
                sb.Append(' ');
                MethodBodyTokenFormatter.FormatTo(symbol, library.Compilation, sb);
                break;
            case string strOp:
                sb.Append(' ');
                sb.Append('"');
                sb.Append(strOp);
                sb.Append('"');
                break;
            case { } operand:
                sb.Append(' ');
                sb.Append(operand);
                break;
            case null:
                break;
            }

            sb.AppendLine();
        }
    }

    private static void WriteBodyProlog(CompiledLibrary library, MetadataMethodSymbol func, MetadataReader reader, MethodBodyBlock body, StringBuilder sb)
    {
        if (!body.LocalSignature.IsNil)
        {
            sb.Append(".maxstack ");
            sb.Append(body.MaxStack);
            sb.AppendLine();

            sb.Append(".locals ");
            if (body.LocalVariablesInitialized)
                sb.Append("init ");

            sb.Append('(');

            var locals = reader.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(library.Compilation.TypeProvider, func);
            for (var i = 0; i < locals.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                MethodBodyTokenFormatter.FormatTo(locals[i], library.Compilation, sb);
            }

            sb.Append(')');
            sb.AppendLine();
            sb.AppendLine();
        }
    }
}
