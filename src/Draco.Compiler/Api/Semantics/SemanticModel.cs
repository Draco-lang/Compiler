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
                _ = func.Parameters.Count();
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
    /// Retrieves the <see cref="ISymbol"/> defined by <paramref name="subtree"/>.
    /// </summary>
    /// <param name="subtree">The tree that is asked for the defined <see cref="ISymbol"/>.</param>
    /// <returns>The defined <see cref="ISymbol"/> by <paramref name="subtree"/>, or null if it does not
    /// define any.</returns>
    public ISymbol? GetDefinedSymbol(SyntaxNode subtree)
    {
        if (!BinderFacts.DefinesSymbol(subtree)) return null;
        // TODO: LOTS OF DUPLICATE CODE THIS IS BAD
        // JUST LOOK AT THE FUNCTION BELOW
        // ELIMINATE SOME OF THIS GARBAGE PLEASE
        var binder = this.compilation.GetBinder(subtree);
        if (binder.ContainingSymbol is SourceFunctionSymbol functionSymbol)
        {
            if (subtree is FunctionDeclarationSyntax)
            {
                // It's just the containing symbol
                return functionSymbol.ToApiSymbol();
            }

            if (subtree is ParameterSyntax)
            {
                // We can just search in the function symbol
                var parameterSymbol = (Internal.Symbols.ParameterSymbol)functionSymbol.Parameters
                    .Cast<ISourceSymbol>()
                    .First(sym => subtree == sym.DeclarationSyntax);
                return parameterSymbol.ToApiSymbol();
            }

            // TODO: We should somehow get the function to use the incremental binder in this context...
            // Maybe don't expose the body at all?
            // Or should the function symbol know about semantic context?
            // Or define an accessor for body that takes an optional semantic model?
            // var boundBody = functionSymbol.Body;

            if (subtree is TypeSyntax or LabelSyntax)
            {
                // Labels and types bind to a symbol directly
                if (!this.symbolMap.ContainsKey(subtree))
                {
                    var bodyBinder = this.GetBinder(functionSymbol);
                    _ = bodyBinder.BindFunctionBody(functionSymbol.DeclarationSyntax.Body);
                }

                // Now the syntax node should be in the map
                return this.symbolMap[subtree].ToApiSymbol();
            }
            else
            {
                // Expressions and statements are different, they become bound nodes
                if (!this.syntaxMap.ContainsKey(subtree))
                {
                    var bodyBinder = this.GetBinder(functionSymbol);
                    _ = bodyBinder.BindFunctionBody(functionSymbol.DeclarationSyntax.Body);
                }

                // Now the syntax node should be in the map
                var boundNodes = this.syntaxMap[subtree];
                // TODO: We need to deal with potential multiple returns here
                if (boundNodes.Count != 1) throw new NotImplementedException();
                return ExtractDefinedSymbol(boundNodes[0]).ToApiSymbol();
            }
        }
        else if (binder.ContainingSymbol is SourceModuleSymbol module)
        {
            var symbol = (Symbol)module.Members
                .OfType<ISourceSymbol>()
                .First(sym => sym.DeclarationSyntax == subtree);
            return symbol.ToApiSymbol();
        }
        else
        {
            // TODO
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> referenced by <paramref name="subtree"/>.
    /// </summary>
    /// <param name="subtree">The tree that is asked for the referenced <see cref="ISymbol"/>.</param>
    /// <returns>The referenced <see cref="ISymbol"/> by <paramref name="subtree"/>, or null
    /// if it does not reference any.</returns>
    public ISymbol? GetReferencedSymbol(SyntaxNode subtree)
    {
        if (!BinderFacts.ReferencesSymbol(subtree)) return null;
        var binder = this.compilation.GetBinder(subtree);
        if (binder.ContainingSymbol is SourceFunctionSymbol functionSymbol)
        {
            // TODO: We should somehow get the function to use the incremental binder in this context...
            // Maybe don't expose the body at all?
            // Or should the function symbol know about semantic context?
            // Or define an accessor for body that takes an optional semantic model?
            // var boundBody = functionSymbol.Body;

            if (subtree is TypeSyntax or LabelSyntax)
            {
                // Labels and types bind to a symbol directly
                if (!this.symbolMap.ContainsKey(subtree))
                {
                    var bodyBinder = this.GetBinder(functionSymbol);
                    _ = bodyBinder.BindFunctionBody(functionSymbol.DeclarationSyntax.Body);

                    // To make sure, we bind the return type too
                    if (functionSymbol.DeclarationSyntax.ReturnType is not null)
                    {
                        var diagnostics = this.compilation.GlobalDiagnosticBag;
                        _ = bodyBinder.BindType(functionSymbol.DeclarationSyntax.ReturnType.Type, diagnostics);
                    }
                }

                // Now the syntax node should be in the map
                return this.symbolMap[subtree].ToApiSymbol();
            }
            else
            {
                // Expressions and statements are different, they become bound nodes
                if (!this.syntaxMap.ContainsKey(subtree))
                {
                    var bodyBinder = this.GetBinder(functionSymbol);
                    _ = bodyBinder.BindFunctionBody(functionSymbol.DeclarationSyntax.Body);
                }

                // Now the syntax node should be in the map
                var boundNodes = this.syntaxMap[subtree];
                // TODO: We need to deal with potential multiple returns here
                if (boundNodes.Count != 1) throw new NotImplementedException();
                return ExtractReferencedSymbol(boundNodes[0]).ToApiSymbol();
            }
        }
        else if (binder.ContainingSymbol is SourceModuleSymbol module)
        {
            if (!this.syntaxMap.ContainsKey(subtree))
            {
                // We don't have a choice, we need to go through top-level module elements
                // and bind everything incrementally
                var moduleBinder = this.GetBinder(module);
                var diagnostics = this.compilation.GlobalDiagnosticBag;
                foreach (var symbol in module.Members)
                {
                    if (symbol is SourceGlobalSymbol global)
                    {
                        if (global.DeclarationSyntax.Type is not null)
                        {
                            _ = moduleBinder.BindType(global.DeclarationSyntax.Type.Type, diagnostics);
                        }
                        if (global.DeclarationSyntax.Value is not null)
                        {
                            _ = moduleBinder.BindGlobalValue(global.DeclarationSyntax.Value.Value);
                        }
                    }
                    else
                    {
                        // NOTE: Anything else to handle?
                    }
                }
            }

            // TODO: Copypasted from above, maybe just factor out a method to collect bound nodes?
            // Now the syntax node should be in the map
            var boundNodes = this.syntaxMap[subtree];
            // TODO: We need to deal with potential multiple returns here
            if (boundNodes.Count != 1) throw new NotImplementedException();
            return ExtractReferencedSymbol(boundNodes[0]).ToApiSymbol();
        }
        else
        {
            // TODO
            throw new NotImplementedException();
        }
    }

    private Binder GetBinder(Symbol symbol)
    {
        var binder = this.compilation.GetBinder(symbol);
        return new IncrementalBinder(binder, this);
    }

    private static Symbol ExtractDefinedSymbol(BoundNode node) => node switch
    {
        BoundLocalDeclaration l => l.Local,
        BoundLabelStatement l => l.Label,
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };

    private static Symbol ExtractReferencedSymbol(BoundNode node) => node switch
    {
        BoundFunctionExpression f => f.Function,
        BoundParameterExpression p => p.Parameter,
        BoundLocalExpression l => l.Local,
        BoundGlobalExpression g => g.Global,
        BoundReferenceErrorExpression e => e.Symbol,
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };
}
