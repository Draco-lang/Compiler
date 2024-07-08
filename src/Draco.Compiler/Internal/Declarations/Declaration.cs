using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Declarations;

/// <summary>
/// Represents any kind of top-level declaration in the source code.
/// </summary>
internal abstract class Declaration(string name)
{
    /// <summary>
    /// The name of the declared element.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The declarations within this one.
    /// </summary>
    public abstract ImmutableArray<Declaration> Children { get; }

    /// <summary>
    /// The syntaxes that contribute to this declaration.
    /// </summary>
    public abstract IEnumerable<SyntaxNode> DeclaringSyntaxes { get; }
}
