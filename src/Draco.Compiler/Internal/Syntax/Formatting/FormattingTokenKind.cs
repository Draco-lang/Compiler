using System;

namespace Draco.Compiler.Internal.Syntax.Formatting;

[Flags]
internal enum FormattingTokenKind
{
    NoFormatting = 0,
    PadLeft = 1,
    PadRight = 1 << 1,
    PadAround = PadLeft | PadRight,
    TreatAsWhitespace = 1 << 2,
    Semicolon = 1 << 3,
    ExtraNewline = 1 << 4
}
