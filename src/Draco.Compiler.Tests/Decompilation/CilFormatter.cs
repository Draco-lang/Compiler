using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Metadata;
using Draco.Compiler.Tests.Utilities;

namespace Draco.Compiler.Tests.Decompilation;

internal static class CilFormatter
{
    public static string VisualizeIl(CompiledLibrary library, MetadataMethodSymbol func, PEReader peReader, MetadataReader reader)
    {
        var body = peReader.GetMethodBody(func.BodyRelativeVirtualAddress);

        var sb = new StringBuilder();
        var isb = new IndentedStringBuilder(sb);
        isb.AppendLine("{");
        isb.PushIndent();

        // TODO: branching & exception handling
        WriteBodyProlog(library, func, reader, body, isb);
        WriteInstructions(library, func, reader, body, isb);

        isb.PopIndent();
        isb.AppendLine("}");

        return sb.ToString();
    }

    private static unsafe void WriteInstructions(CompiledLibrary library, MetadataMethodSymbol func, MetadataReader reader, MethodBodyBlock body, IndentedStringBuilder sb)
    {
        var blobReader = body.GetILReader();

        var span = new ReadOnlySpan<byte>(blobReader.StartPointer, blobReader.Length);

        var instructions = new List<CilInstruction>();
        instructions.EnsureCapacity(10);

        HashSet<int>? jumpTargets = null;

        while (!span.IsEmpty)
        {
            var instruction = InstructionDecoder.Read(span, blobReader.Length - span.Length, library.Codegen.GetSymbol, reader.GetUserString, out var advance);
            span = span[advance..];

            if (InstructionDecoder.IsBranch(instruction.OpCode))
            {
                jumpTargets ??= new();
                jumpTargets.Add(((IConvertible)instruction.Operand!).ToInt32(null));
            }

            instructions.Add(instruction);
        }

        foreach (var (opCode, offset, operand) in instructions)
        {
            foreach (var region in body.ExceptionRegions)
                if (region.TryOffset == offset)
                {
                    sb.AppendLine(".try {");
                    sb.PushIndent();
                }
                else if (region.HandlerOffset == offset)
                    switch (region.Kind)
                    {
                    case ExceptionRegionKind.Catch:
                        break;
                    case ExceptionRegionKind.Filter:
                        break;
                    case ExceptionRegionKind.Finally:
                        sb.AppendLine("finally {");
                        sb.PushIndent();
                        break;
                    case ExceptionRegionKind.Fault:
                        break;
                    }

            if (jumpTargets is { } && jumpTargets.Contains(offset))
                using (sb.WithDedent())
                {
                    sb.Append("IL_");
                    sb.Append(offset.ToString("X4"));
                    sb.AppendLine(":");
                }

            sb.Append(InstructionDecoder.GetText(opCode));

            switch (operand)
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
            case { } when InstructionDecoder.IsBranch(opCode):
                sb.Append(" IL_");
                sb.AppendLine(((IFormattable)operand).ToString("X4", null));
                break;
            case { }:
                sb.Append(' ');
                sb.Append(operand);
                break;
            case null:
                break;
            }

            sb.AppendLine();

            var opCodeEndOffset = offset + InstructionDecoder.GetTotalOpCodeSize(opCode);

            foreach (var region in body.ExceptionRegions)
            {
                if (region.TryOffset + region.TryLength == opCodeEndOffset
                || region.HandlerOffset + region.HandlerLength == opCodeEndOffset)
                {
                    sb.PopIndent();
                    sb.AppendLine("}");
                }
            }
        }
    }

    private static void WriteBodyProlog(CompiledLibrary library, MetadataMethodSymbol func, MetadataReader reader, MethodBodyBlock body, IndentedStringBuilder sb)
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
