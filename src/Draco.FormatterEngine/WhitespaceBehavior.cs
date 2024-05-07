using System;

namespace Draco.Compiler.Internal.Syntax.Formatting;

/// <summary>
/// Whitespace behavior that will be respected when formatting the file.
/// </summary>
[Flags]
public enum WhitespaceBehavior
{
    /// <summary>
    /// No formatting behavior is set. Unspecified behavior.
    /// </summary>
    NoFormatting = 0,
    /// <summary>
    /// Add a left whitespace if necessary.
    /// </summary>
    PadLeft = 1 << 0,
    /// <summary>
    /// Add a right whitespace if necessary
    /// </summary>
    PadRight = 1 << 1,
    /// <summary>
    /// The next token will think of this token as a whitespace.
    /// </summary>
    BehaveAsWhiteSpaceForNextToken = 1 << 3,
    /// <summary>
    /// The previous token will think of this as a whitespace.
    /// </summary>
    BehaveAsWhiteSpaceForPreviousToken = 1 << 4,
    /// <summary>
    /// Remove one indentation level.
    /// </summary>
    RemoveOneIndentation = 1 << 6,
    /// <summary>
    /// Add a whitespace on the left and right, if necessary.
    /// </summary>
    PadAround = PadLeft | PadRight,
    /// <summary>
    /// This token behave as a whitespace.
    /// </summary>
    Whitespace = BehaveAsWhiteSpaceForNextToken | BehaveAsWhiteSpaceForPreviousToken,
}
