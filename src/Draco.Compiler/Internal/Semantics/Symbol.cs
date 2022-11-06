using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// The base of all symbols.
/// </summary>
/// <param name="Name">The name of the symbol.</param>
internal abstract record class Symbol(string Name) : ISymbol
{
    /// <summary>
    /// A symbol for a label.
    /// </summary>
    /// <param name="Name">The name of the label.</param>
    public sealed record class Label(string Name) : Symbol(Name);

    /// <summary>
    /// A symbol for a function declaration.
    /// </summary>
    /// <param name="Name">The name of the function.</param>
    public sealed record class Function(string Name) : Symbol(Name);

    /// <summary>
    /// A symbol for a variable declaration.
    /// </summary>
    /// <param name="IsMutable">True, if the variable is mutable.</param>
    /// <param name="Name">The name of the variable.</param>
    public sealed record class Variable(bool IsMutable, string Name) : Symbol(Name);
}
