using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
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
}
