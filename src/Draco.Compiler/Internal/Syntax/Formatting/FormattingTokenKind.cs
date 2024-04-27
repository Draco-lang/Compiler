using System;

namespace Draco.Compiler.Internal.Syntax.Formatting;

[Flags]
internal enum FormattingTokenKind
{
    NoFormatting = 0,
    PadLeft = 1,
    PadRight = 1 << 1,
    ForceRightPad = 1 << 2,
    BehaveAsWhiteSpaceForNextToken = 1 << 3,
    BehaveAsWhiteSpaceForPreviousToken = 1 << 4,
    ExtraNewline = 1 << 5,
    RemoveOneIndentation = 1 << 6,
    PadAround = PadLeft | PadRight,
    Whitespace = BehaveAsWhiteSpaceForNextToken | BehaveAsWhiteSpaceForPreviousToken,
}
