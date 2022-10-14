using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Syntax;

/// <summary>
/// A source of <see cref="Token"/>s.
/// </summary>
internal interface ITokenSource
{
    /// <summary>
    /// Peeks ahead <paramref name="offset"/> of tokens in the source without consuming it.
    /// </summary>
    /// <param name="offset">The offset from the current source position.</param>
    /// <returns>The <see cref="Token"/> that is <paramref name="offset"/> amount of tokens ahead.</returns>
    public Token Peek(int offset = 0);

    /// <summary>
    /// Advances in the source <paramref name="amount"/> amount of tokens.
    /// </summary>
    /// <param name="amount">The amount of tokens to advance.</param>
    public void Advance(int amount = 1);
}

/// <summary>
/// Factory functions for constructing <see cref="ITokenSource"/>s.
/// </summary>
internal static class TokenSource
{
    // TODO
}
