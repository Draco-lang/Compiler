using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Syntax;

/// <summary>
/// Represents a single token from source code.
/// </summary>
/// <param name="Type">The <see cref="TokenType"/> this token is categorized as.</param>
/// <param name="Text">The text this token was constructed from.</param>
public readonly record struct Token(TokenType Type, ReadOnlyMemory<char> Text)
{
    /// <summary>
    /// True, if this <see cref="Token"/> counts as trivia.
    /// </summary>
    public bool IsTrivia => this.Type.IsTrivia();
}
