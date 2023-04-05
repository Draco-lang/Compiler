using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.FlowAnalysis;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Api.Semantics;

/// <summary>
/// The semantic model of a subtree.
/// </summary>
public sealed partial class SemanticModel
{
    /// <summary>
    /// The the tree that the semantic model is for.
    /// </summary>
    public SyntaxTree Tree { get; }

    /// <summary>
    /// All <see cref="Diagnostic"/>s in this model.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics => this.diagnostics ??= this.GetAllDiagnostics();
    private ImmutableArray<Diagnostic>? diagnostics;

    private readonly Compilation compilation;
    private readonly Dictionary<SyntaxNode, IList<BoundNode>> syntaxMap = new();
    private readonly Dictionary<SyntaxNode, Symbol> symbolMap = new();

    internal SemanticModel(Compilation compilation, SyntaxTree tree)
    {
        this.Tree = tree;
        this.compilation = compilation;
    }

    // TODO: This isn't exactly retrieving the diags only for this tree...
    /// <summary>
    /// Retrieves all semantic <see cref="Diagnostic"/>s.
    /// </summary>
    /// <returns>All <see cref="Diagnostic"/>s produced during semantic analysis.</returns>
    private ImmutableArray<Diagnostic> GetAllDiagnostics()
    {
        var result = ImmutableArray.CreateBuilder<Diagnostic>();

        // Retrieve all syntax errors
        var syntaxDiagnostics = this.compilation.SyntaxTrees.SelectMany(tree => tree.Diagnostics);
        result.AddRange(syntaxDiagnostics);

        // Next, we enforce binding everywhere
        foreach (var symbol in this.compilation.GlobalModule.Members)
        {
            if (symbol is SourceFunctionSymbol func)
            {
                _ = func.Parameters;
                _ = func.ReturnType;
                // Avoid double-evaluation of diagnostics
                if (!this.syntaxMap.ContainsKey(func.DeclarationSyntax.Body)) _ = func.Body;
            }
            else if (symbol is SourceGlobalSymbol global)
            {
                _ = global.Type;
                _ = global.Value;
            }
        }

        // Dump back all diagnostics
        result.AddRange(this.compilation.GlobalDiagnosticBag);

        // Dump in DFA analysis results
        var dfaDiags = DataFlowPasses.Analyze((SourceModuleSymbol)this.compilation.GlobalModule);
        result.AddRange(dfaDiags);

        return result.ToImmutable();
    }

    // NOTE: These OrNull functions are not too pretty
    // For now public API is not that big of a concern, so they can stay
    // Instead we could just always return a nullable or an error symbol when appropriate

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> defined by <paramref name="syntax"/>.
    /// </summary>
    /// <param name="syntax">The tree that is asked for the defined <see cref="ISymbol"/>.</param>
    /// <returns>The defined <see cref="ISymbol"/> by <paramref name="syntax"/>, or null if it does not
    /// define any.</returns>
    public ISymbol? GetDefinedSymbol(SyntaxNode syntax)
    {
        if (!BinderFacts.DefinesSymbol(syntax)) return null;

        // We look up syntax based on the symbol in context
        var binder = this.compilation.GetBinder(syntax);
        switch (binder.ContainingSymbol)
        {
        case SourceFunctionSymbol function:
        {
            // Check if it's the function itself
            if (function.DeclarationSyntax == syntax) return function.ToApiSymbol();

            // If it's a parameter syntax, it's within the function
            if (syntax is ParameterSyntax)
            {
                // We can just search in the function symbol
                var parameterSymbol = function.Parameters
                    .First(sym => syntax == sym.DeclarationSyntax);
                return parameterSymbol.ToApiSymbol();
            }

            // As a last resort, we look into the function body
            // First, check if the syntax node is already cached
            if (!this.syntaxMap.ContainsKey(syntax))
            {
                // If not, bind it
                var diagnostics = this.compilation.GlobalDiagnosticBag;
                var bodyBinder = this.GetBinder(function);
                _ = bodyBinder.BindFunction(function, diagnostics);
            }

            // Now the syntax node should be in the map
            if (!this.syntaxMap.TryGetValue(syntax, out var boundNodes)) return null;

            // TODO: We need to deal with potential multiple returns here
            if (boundNodes.Count != 1) throw new NotImplementedException();

            // Just return the singleton symbol
            return ExtractDefinedSymbol(boundNodes[0]).ToApiSymbol();
        }
        case SourceModuleSymbol module:
        {
            // We try to look up the module-level declarations
            var symbol = module.Members
                .FirstOrDefault(sym => sym.DeclarationSyntax == syntax);
            return symbol?.ToApiSymbol();
        }
        default:
            return null;
        }
    }

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> referenced by <paramref name="syntax"/>.
    /// </summary>
    /// <param name="syntax">The tree that is asked for the referenced <see cref="ISymbol"/>.</param>
    /// <returns>The referenced <see cref="ISymbol"/> by <paramref name="syntax"/>, or null
    /// if it does not reference any.</returns>
    public ISymbol? GetReferencedSymbol(SyntaxNode syntax)
    {
        if (!BinderFacts.ReferencesSymbol(syntax)) return null;

        // We look up syntax based on the symbol in context
        var binder = this.compilation.GetBinder(syntax);
        switch (binder.ContainingSymbol)
        {
        case SourceFunctionSymbol function:
        {
            // If the syntax binds to a symbol directly, we check that map
            if (BindsToSymbol(syntax))
            {
                // If not cached, bind the function body
                if (!this.symbolMap.ContainsKey(syntax))
                {
                    var diagnostics = this.compilation.GlobalDiagnosticBag;

                    var bodyBinder = this.GetBinder(function);
                    _ = bodyBinder.BindFunction(function, diagnostics);

                    // Since the parameter types and the return type are in this scope too, bind them
                    var functionSyntax = function.DeclarationSyntax;
                    // Parameters
                    foreach (var param in functionSyntax.ParameterList.Values)
                    {
                        _ = bodyBinder.BindType(param.Type, diagnostics);
                    }
                    // Return type
                    if (functionSyntax.ReturnType is not null)
                    {
                        _ = bodyBinder.BindType(functionSyntax.ReturnType.Type, diagnostics);
                    }
                }

                // Now the syntax node should be in the map
                return this.symbolMap.TryGetValue(syntax, out var symbol)
                    ? symbol.ToApiSymbol()
                    : null;
            }
            else
            {
                // Binds to a bound node
                // If not cached, bind function body
                if (!this.syntaxMap.ContainsKey(syntax))
                {
                    var diagnostics = this.compilation.GlobalDiagnosticBag;
                    var bodyBinder = this.GetBinder(function);
                    _ = bodyBinder.BindFunction(function, diagnostics);
                }

                // Now the syntax node should be in the map
                if (!this.syntaxMap.TryGetValue(syntax, out var boundNodes)) return null;

                // TODO: We need to deal with potential multiple returns here
                if (boundNodes.Count != 1) throw new NotImplementedException();

                // Just return the singleton symbol
                return ExtractReferencedSymbol(boundNodes[0])?.ToApiSymbol();
            }
        }
        case SourceModuleSymbol module:
        {
            var isInMap = BindsToSymbol(syntax)
                ? this.symbolMap.ContainsKey(syntax)
                : this.syntaxMap.ContainsKey(syntax);
            if (!isInMap)
            {
                // We don't have a choice, we need to go through top-level module elements
                // and bind everything incrementally
                var moduleBinder = this.GetBinder(module);
                var diagnostics = this.compilation.GlobalDiagnosticBag;
                foreach (var symbol in module.Members)
                {
                    switch (symbol)
                    {
                    case SourceGlobalSymbol global:
                    {
                        // Bind type and value
                        moduleBinder.BindGlobal(global, diagnostics);
                        break;
                    }
                    // NOTE: Anything else to handle?
                    }
                }
            }

            // Now the syntax node should be in the appropriate map
            if (BindsToSymbol(syntax))
            {
                return this.symbolMap.TryGetValue(syntax, out var symbol)
                    ? symbol.ToApiSymbol()
                    : null;
            }
            else
            {
                if (!this.syntaxMap.TryGetValue(syntax, out var boundNodes)) return null;

                // TODO: We need to deal with potential multiple returns here
                if (boundNodes.Count != 1) throw new NotImplementedException();

                // Just return the singleton symbol
                return ExtractReferencedSymbol(boundNodes[0])?.ToApiSymbol();
            }
        }
        default:
            return null;
        }
    }

    private Binder GetBinder(Symbol symbol)
    {
        var binder = this.compilation.GetBinder(symbol);
        return new IncrementalBinder(binder, this);
    }

    private static bool BindsToSymbol(SyntaxNode syntax) => syntax is TypeSyntax or LabelSyntax;

    private static Symbol ExtractDefinedSymbol(BoundNode node) => node switch
    {
        BoundLocalDeclaration l => l.Local,
        BoundLabelStatement l => l.Label,
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };

    private static Symbol? ExtractReferencedSymbol(BoundNode node) => node switch
    {
        BoundFunctionExpression f => f.Function,
        BoundParameterExpression p => p.Parameter,
        BoundLocalExpression l => l.Local,
        BoundGlobalExpression g => g.Global,
        BoundReferenceErrorExpression e => e.Symbol,
        BoundLocalLvalue l => l.Local,
        BoundGlobalLvalue g => g.Global,
        BoundIllegalLvalue => null,
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };
}
