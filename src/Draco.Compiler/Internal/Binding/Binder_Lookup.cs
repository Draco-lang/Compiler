using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Looks up a symbol that can be used in value-context.
    /// </summary>
    /// <param name="name">The name of the symbol to look up.</param>
    /// <param name="reference">The syntax referencing the symbol.</param>
    /// <param name="diagnostics">The diagnostics are added here from lookup.</param>
    /// <returns>The looked up symbol, which might represent an error.</returns>
    protected Symbol LookupValueSymbol(string name, SyntaxNode reference, DiagnosticBag diagnostics)
    {
        var result = this.LookupInternal(name, BinderFacts.IsValueSymbol, reference);
        return result.GetValue(reference, diagnostics);
    }

    /// <summary>
    /// Looks up a symbol that can be used in type-context.
    /// </summary>
    /// <param name="name">The name of the symbol to look up.</param>
    /// <param name="reference">The syntax referencing the symbol.</param>
    /// <param name="diagnostics">The diagnostics are added here from lookup.</param>
    /// <returns>The looked up symbol, which might represent an error.</returns>
    protected TypeSymbol LookupTypeSymbol(string name, SyntaxNode reference, DiagnosticBag diagnostics)
    {
        var result = this.LookupInternal(name, BinderFacts.IsTypeSymbol, reference);
        return result.GetType(reference, diagnostics);
    }

    /// <summary>
    /// Looks up a symbol that can be used in label-context.
    /// </summary>
    /// <param name="name">The name of the symbol to look up.</param>
    /// <param name="reference">The syntax referencing the symbol.</param>
    /// <param name="diagnostics">The diagnostics are added here from lookup.</param>
    /// <returns>The looked up symbol, which might represent an error.</returns>
    protected LabelSymbol LookupLabelSymbol(string name, SyntaxNode reference, DiagnosticBag diagnostics)
    {
        var result = this.LookupInternal(name, BinderFacts.IsLabelSymbol, reference);
        return result.GetLabel(reference, diagnostics);
    }

    /// <summary>
    /// Looks up a symbol starting with this binder.
    /// </summary>
    /// <param name="name">The name of the symbol to look up.</param>
    /// <param name="allowSymbol">A predicate to filter for allowed symbols.</param>
    /// <param name="reference">The referencing syntax.</param>
    /// <returns>The lookup result.</returns>
    private LookupResult LookupInternal(string name, Predicate<Symbol> allowSymbol, SyntaxNode reference)
    {
        // First, we create the result that holds the info
        var lookupResult = new LookupResult();

        // Set up state
        var flags = LookupFlags.None;
        var currentReference = reference;

        // Iterate over the binder chain
        for (var scope = this; scope is not null; scope = scope.Parent)
        {
            if (!lookupResult.ShouldContinue) break;

            // Look up in the current scope
            scope.LookupLocal(lookupResult, name, ref flags, allowSymbol, ref currentReference);
        }

        return lookupResult;
    }

    /// <summary>
    /// Retrieves symbols to be looked up from this binders scope only.
    /// </summary>
    /// <param name="result">The lookup result to write into.</param>
    /// <param name="name">The name of the symbol to look up.</param>
    /// <param name="flags">The active lookup flags.</param>
    /// <param name="allowSymbol">A predicate to check, if the symbol is allowed to be looked up.</param>
    /// <param name="currentReference">The syntax that references the symbol in the current scope.</param>
    protected virtual void LookupLocal(
        LookupResult result,
        string name,
        ref LookupFlags flags,
        Predicate<Symbol> allowSymbol,
        ref SyntaxNode? currentReference)
    {
    }
}
