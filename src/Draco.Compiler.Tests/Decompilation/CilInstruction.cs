using System.Reflection.Metadata;

namespace Draco.Compiler.Tests.Decompilation;

internal readonly record struct CilInstruction(ILOpCode OpCode, object? Operand);
