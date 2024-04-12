using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

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
    internal Symbol LookupValueSymbol(string name, SyntaxNode reference, DiagnosticBag diagnostics)
    {
        var result = this.LookupInternal(name, BinderFacts.IsValueSymbol, reference);
        return result.GetValue(name, reference, diagnostics);
    }

    /// <summary>
    /// Looks up a symbol that can be used in non-type value context.
    /// </summary>
    /// <param name="name">The name of the symbol to look up.</param>
    /// <param name="reference">The syntax referencing the symbol.</param>
    /// <param name="diagnostics">The diagnostics are added here from lookup.</param>
    /// <returns>The looked up symbol, which might represent an error.</returns>
    internal Symbol LookupNonTypeValueSymbol(string name, SyntaxNode reference, DiagnosticBag diagnostics)
    {
        var result = this.LookupInternal(name, BinderFacts.IsNonTypeValueSymbol, reference);
        return result.GetValue(name, reference, diagnostics);
    }

    /// <summary>
    /// Looks up a symbol that can be used in type-context.
    /// </summary>
    /// <param name="name">The name of the symbol to look up.</param>
    /// <param name="reference">The syntax referencing the symbol.</param>
    /// <param name="diagnostics">The diagnostics are added here from lookup.</param>
    /// <returns>The looked up symbol, which might represent an error.</returns>
    internal Symbol LookupTypeSymbol(string name, SyntaxNode reference, DiagnosticBag diagnostics)
    {
        var result = this.LookupInternal(name, BinderFacts.IsTypeSymbol, reference);
        return result.GetType(name, reference, diagnostics);
    }

    /// <summary>
    /// Looks up a symbol that can be used in label-context.
    /// </summary>
    /// <param name="name">The name of the symbol to look up.</param>
    /// <param name="reference">The syntax referencing the symbol.</param>
    /// <param name="diagnostics">The diagnostics are added here from lookup.</param>
    /// <returns>The looked up symbol, which might represent an error.</returns>
    internal LabelSymbol LookupLabelSymbol(string name, SyntaxNode reference, DiagnosticBag diagnostics)
    {
        var result = this.LookupInternal(name, BinderFacts.IsLabelSymbol, reference);
        return result.GetLabel(name, reference, diagnostics);
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
        // NOTE: The order of setting the reference and parent is CORRECT here
        // In the parent scope, the child syntax will play the referencing role
        for (var scope = this; scope is not null; currentReference = scope.DeclaringSyntax, scope = scope.Parent)
        {
            if (!lookupResult.ShouldContinue) break;

            // Look up in the current scope
            scope.LookupLocal(lookupResult, name, ref flags, allowSymbol, currentReference);
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
    internal virtual void LookupLocal(
        LookupResult result,
        string name,
        ref LookupFlags flags,
        Predicate<Symbol> allowSymbol,
        SyntaxNode? currentReference)
    {
    }

    /// <summary>
    /// Looks up an operator with the given name and operand types.
    /// </summary>
    /// <param name="name">The name of the operator to look up.</param>
    /// <param name="syntax">The referencing syntax.</param>
    /// <param name="operands">The operand types.</param>
    /// <param name="constraints">The constraint solver to use.</param>
    /// <returns>The looked up operator task.</returns>
    private async SolverTask<FunctionSymbol> LookupOperator(
        string name,
        SyntaxNode syntax,
        ImmutableArray<TypeSymbol> operands,
        ConstraintSolver constraints)
    {
        // First, we try to look up globally
        // We can ignore lookup errors here, missing overloads will report it either way
        var fromGlobal = this.LookupValueSymbol(name, syntax, DiagnosticBag.Empty);

        // Then we try to look up in any of the operands
        // Again, we make this silent as well, the lookup error will be reported by the overload resolution
        var fromOperands = await SolverTask.WhenAll(operands.Select(operandType => constraints.Member(
            operandType,
            name,
            out _,
            syntax
            // TODO: Check if we still need this
            /*, silent: true*/)));

        // Merge all the found functions
        var functions = GetFunctionsMerged(fromOperands.Append(fromGlobal));

        // Actually start to resolve the overload
        return await constraints.Overload(
            name,
            functions,
            operands
                .Select(op => constraints.Arg(null, op))
                .ToImmutableArray(),
            out _,
            syntax);
    }

    /// <summary>
    /// Retrieves the functions from the given symbol.
    /// </summary>
    /// <param name="symbol">The symbol to extract the functions from.</param>
    /// <returns>The extracted array of functions.</returns>
    private static ImmutableArray<FunctionSymbol> GetFunctions(Symbol symbol) =>
        GetFunctionsImpl(symbol).ToImmutableArray();

    /// <summary>
    /// Retrieves the functions from the given symbols, merging them into one.
    /// </summary>
    /// <param name="symbols">The symbols to extract the functions from.</param>
    /// <returns>The extracted array of distinct functions.</returns>
    private static ImmutableArray<FunctionSymbol> GetFunctionsMerged(IEnumerable<Symbol> symbols) => symbols
        .SelectMany(GetFunctionsImpl)
        .Distinct(SymbolEqualityComparer.Default)
        .ToImmutableArray();

    // Helper for function symbol extraction
    private static IEnumerable<FunctionSymbol> GetFunctionsImpl(Symbol symbol) => symbol switch
    {
        FunctionSymbol f => new[] { f },
        OverloadSymbol o => o.Functions,
        _ => Enumerable.Empty<FunctionSymbol>(),
    };
}
