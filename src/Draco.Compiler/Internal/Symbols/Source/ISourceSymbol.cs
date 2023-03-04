using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// Represents a symbol that's defined in-source.
/// </summary>
internal interface ISourceSymbol
{
    /// <summary>
    /// The syntax declaring this symbol.
    /// </summary>
    public SyntaxNode? DeclarationSyntax { get; }
}
