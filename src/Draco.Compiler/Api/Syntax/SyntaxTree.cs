using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Represents the entire syntax tree of a Draco source file.
/// </summary>
public sealed class SyntaxTree
{
    /// <summary>
    /// The root <see cref="SyntaxNode"/> of the tree.
    /// </summary>
    public SyntaxNode Root => throw new NotImplementedException();

    private readonly Internal.Syntax.SyntaxTree greenTree;

    internal SyntaxTree(Internal.Syntax.SyntaxTree greenTree)
    {
        this.greenTree = greenTree;
    }
}
