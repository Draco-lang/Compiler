using System.Buffers.Binary;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Draco.Compiler.Tests.Decompilation;

internal static class InstructionDecoder
{
    public static string GetText(ILOpCode code) => s_opCodes[(ushort)code].Name!;

    public static CilInstruction Read(ReadOnlySpan<byte> ilStream, Func<EntityHandle, object?> resolveToken, Func<UserStringHandle, string> resolveString, out int advance)
    {
        ushort id = ilStream[0];

        var opCodeSize = 1;

        if (IsWideInstruction(id))
        {
            id = BinaryPrimitives.ReadUInt16BigEndian(ilStream);
            opCodeSize++;
        }

        var operandType = GetOperandType((ILOpCode)id);

        var operand = ReadOperand(operandType, ilStream[opCodeSize..], resolveToken, resolveString);

        advance = opCodeSize + GetOperandSize(operandType);

        return new CilInstruction((ILOpCode)id, operand);
    }

    private static object? ReadOperand(OperandType type, ReadOnlySpan<byte> span, Func<EntityHandle, object?> resolveToken, Func<UserStringHandle, string> resolveString)
    {
        return type switch
        {
            OperandType.InlineBrTarget => BinaryPrimitives.ReadInt32LittleEndian(span),
            OperandType.ShortInlineBrTarget => span[0],

            OperandType.ShortInlineVar or
            OperandType.ShortInlineI => span[0],

            OperandType.InlineVar => BinaryPrimitives.ReadInt16LittleEndian(span),

            OperandType.InlineI or
            OperandType.InlineSwitch => BinaryPrimitives.ReadInt32LittleEndian(span),
            OperandType.InlineI8 => BinaryPrimitives.ReadInt64LittleEndian(span),

            // standalone signature can describe locals or 'calli' instruction, but in this context it's only 'calli'
            OperandType.InlineSig or
            OperandType.InlineMethod or
            OperandType.InlineTok or
            OperandType.InlineType or
            OperandType.InlineField => resolveToken(MetadataTokens.EntityHandle(BinaryPrimitives.ReadInt32LittleEndian(span))),

            OperandType.InlineString => resolveString(MetadataTokens.UserStringHandle(BinaryPrimitives.ReadInt32LittleEndian(span))),

            OperandType.InlineR => BinaryPrimitives.ReadDoubleLittleEndian(span),
            OperandType.ShortInlineR => BinaryPrimitives.ReadSingleLittleEndian(span),

            OperandType.InlineNone => null,

            _ => throw new NotSupportedException(),
        };
    }

    private static bool IsWideInstruction(ushort code) => s_opCodes[code].OpCodeType is OpCodeType.Nternal;

    private static OperandType GetOperandType(ILOpCode code) => s_opCodes[(ushort)code].OperandType;

    private static int GetOperandSize(OperandType type) => type switch
    {
        OperandType.InlineBrTarget => 4,
        OperandType.InlineField => 4,
        OperandType.InlineI => 4,
        OperandType.InlineI8 => 8,
        OperandType.InlineMethod => 4,
        OperandType.InlineNone => 0,
        OperandType.InlineR => 8,
        OperandType.InlineSig => 4,
        OperandType.InlineString => 4,
        OperandType.InlineSwitch => 4,
        OperandType.InlineTok => 4,
        OperandType.InlineType => 4,
        OperandType.InlineVar => 2,
        OperandType.ShortInlineBrTarget => 1,
        OperandType.ShortInlineI => 1,
        OperandType.ShortInlineR => 4,
        OperandType.ShortInlineVar => 1,
        //OperandType.InlinePhi // reserved in spec
        _ => throw new NotSupportedException(),
    };

    private static readonly Dictionary<ushort, OpCode> s_opCodes =
        typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (OpCode)f.GetValue(null)!)
            .ToDictionary(o => (ushort)o.Value, o => o);

}
