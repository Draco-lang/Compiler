using System.Reflection.Metadata;

namespace Draco.Compiler.Tests.Decompilation;

internal readonly record struct CilInstruction(ILOpCode OpCode, int Offset, object? Operand);
