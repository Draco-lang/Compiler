using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Represents a single scope that binds the syntax-tree to the untyped-tree and then the bound-tree.
/// </summary>
internal abstract partial class Binder
{
    /// <summary>
    /// The compilation this binder was created for.
    /// </summary>
    protected Compilation Compilation { get; }

    /// <summary>
    /// The parent binder of this one.
    /// </summary>
    protected Binder? Parent { get; }

    protected Binder(Compilation compilation)
    {
        this.Compilation = compilation;
    }

    protected Binder(Binder parent)
        : this(parent.Compilation)
    {
        this.Parent = parent;
    }

    /// <summary>
    /// Attempts to look up a symbol that can be used in value-context (like a function or a variable).
    /// </summary>
    /// <param name="name">The name of the symbol to look up.</param>
    /// <param name="reference">The syntax referencing the symbol.</param>
    /// <returns>The result of the lookup.</returns>
    protected LookupResult LookupValueSymbol(string name, SyntaxNode? reference)
    {
        var result = new LookupResult();
        this.LookupValueSymbol(result, name, reference);
        return result;
    }

    /// <summary>
    /// Attempts to look up a symbol that can be used in type-context (mainly type-definitions).
    /// </summary>
    /// <param name="name">The name of the symbol to look up.</param>
    /// <param name="reference">The syntax referencing the symbol.</param>
    /// <returns>The result of the lookup.</returns>
    protected LookupResult LookupTypeSymbol(string name, SyntaxNode? reference)
    {
        var result = new LookupResult();
        this.LookupTypeSymbol(result, name, reference);
        return result;
    }

    /// <summary>
    /// Attempts to look up a symbol that can be used in value-context (like a function or a variable).
    /// </summary>
    /// <param name="result">The result of the lookup.</param>
    /// <param name="name">The name of the symbol to look up.</param>
    /// <param name="reference">The syntax referencing the symbol.</param>
    protected abstract void LookupValueSymbol(LookupResult result, string name, SyntaxNode? reference);

    /// <summary>
    /// Attempts to look up a symbol that can be used in type-context (mainly type-definitions).
    /// </summary>
    /// <param name="result">The result of the lookup.</param>
    /// <param name="name">The name of the symbol to look up.</param>
    /// <param name="reference">The syntax referencing the symbol.</param>
    protected abstract void LookupTypeSymbol(LookupResult result, string name, SyntaxNode? reference);

    /// <summary>
    /// Checks, if a symbol can be used in value-context.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>True, if <paramref name="symbol"/> can be used in a value-context.</returns>
    protected static bool IsValueSymbol(Symbol symbol) => symbol
        is VariableSymbol
        or FunctionSymbol;

    /// <summary>
    /// Checks, if a symbol can be used in type-context.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>True, if <paramref name="symbol"/> can be used in a type-context.</returns>
    protected static bool IsTypeSymbol(Symbol symbol) => !IsValueSymbol(symbol);
}
