using System;

namespace Draco.Compiler.Internal.Syntax.Formatting;

[Flags]
public enum WhitespaceBehavior
{
    NoFormatting = 0,
    PadLeft = 1 << 0,
    PadRight = 1 << 1,
    ForceRightPad = 1 << 2,
    BehaveAsWhiteSpaceForNextToken = 1 << 3,
    BehaveAsWhiteSpaceForPreviousToken = 1 << 4,
    RemoveOneIndentation = 1 << 6,
    PadAround = PadLeft | PadRight,
    Whitespace = BehaveAsWhiteSpaceForNextToken | BehaveAsWhiteSpaceForPreviousToken,
}
