using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Emit;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.FlowAnalysis;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.UntypedTree;

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
    public ImmutableArray<Diagnostic> Diagnostics => this.diagnostics ??= this.GetDiagnostics();
    private ImmutableArray<Diagnostic>? diagnostics;

    private readonly Compilation compilation;

    // Filled out by incremental binding
    private readonly Dictionary<SyntaxNode, UntypedNode> untypedNodeMap = new();
    private readonly Dictionary<UntypedNode, BoundNode> boundNodeMap = new();
    private readonly Dictionary<SyntaxNode, Symbol> symbolMap = new();

    internal SemanticModel(Compilation compilation, SyntaxTree tree)
    {
        this.Tree = tree;
        this.compilation = compilation;
    }

    /// <summary>
    /// Retrieves all <see cref="Diagnostic"/>s.
    /// </summary>
    /// <param name="span">The span to retrieve the diagnostics in. If null, it retrieves all diagnostics
    /// regardless of the location.</param>
    /// <returns>All <see cref="Diagnostic"/>s for <see cref="Tree"/>.</returns>
    private ImmutableArray<Diagnostic> GetDiagnostics(SourceSpan? span = null)
    {
        var diagnostics = new DiagnosticBag();

        var syntaxNodes = span is null
            ? this.Tree.PreOrderTraverse()
            : this.Tree.TraverseSubtreesIntersectingSpan(span.Value);

        foreach (var syntaxNode in syntaxNodes)
        {
            // Add syntax diagnostics
            var syntaxDiagnostics = this.Tree.SyntaxDiagnosticTable.Get(syntaxNode);
            diagnostics.AddRange(syntaxDiagnostics);

            // Get the symbol this embodies
            var binder = this.GetBinder(syntaxNode);
            var containingSymbol = binder.ContainingSymbol as ISourceSymbol;

            // We want exact syntax matches to avoid duplication
            // This is a cheap way to not to attempt finding a function symbol from its body
            // and from its signature
            switch (syntaxNode)
            {
            case CompilationUnitSyntax:
            case FunctionDeclarationSyntax:
            {
                containingSymbol?.Bind(diagnostics);
                break;
            }
            // NOTE: Only globals need binding
            case VariableDeclarationSyntax when containingSymbol is SourceModuleSymbol containingModule:
            {
                // We need to search for this global
                var globalSymbol = containingModule.Members
                    .OfType<SourceGlobalSymbol>()
                    .Single(s => s.DeclaringSyntax == syntaxNode);
                globalSymbol.Bind(diagnostics);
                break;
            }
            }

            // If it's an import syntax, we need special handling
            if (syntaxNode is ImportDeclarationSyntax importSyntax)
            {
                // TODO
            }
        }

        // For functions:
        //  - parameters
        //  - return type
        //  - body
        //  - flow passes
        //    - return on all paths
        //    - definite assignment
        //    - val assignment
        //  - recurse into local functions

        // For globals:
        //  - type
        //  - value
        //  - flow passes
        //    - definite assignment
        //    - val assignment
        //  - recurse into local functions

        // For every scope:
        //  - imports

        return diagnostics.ToImmutableArray();
    }

    // NOTE: These OrNull functions are not too pretty
    // For now public API is not that big of a concern, so they can stay
    // Instead we could just always return a nullable or an error symbol when appropriate

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> declared by <paramref name="syntax"/>.
    /// </summary>
    /// <param name="syntax">The tree that is asked for the defined <see cref="ISymbol"/>.</param>
    /// <returns>The defined <see cref="ISymbol"/> by <paramref name="syntax"/>, or null if it does not
    /// declared any.</returns>
    public ISymbol? GetDeclaredSymbol(SyntaxNode syntax)
    {
        // Get enclosing context
        var binder = this.GetBinder(syntax);
        var containingSymbol = binder.ContainingSymbol;

        switch (containingSymbol)
        {
        case SourceFunctionSymbol func:
        {
            // This is just the function itself
            if (func.DeclaringSyntax == syntax) return containingSymbol.ToApiSymbol();

            // TODO: Use func.Bind instead, but pass in binder provider
            // Assume contents of the function, bind the function
            var functionBinder = this.GetBinder(func);
            functionBinder.BindFunction(func, this.compilation.GlobalDiagnosticBag);
            // Look up inside the binder
            var symbol = binder.DeclaredSymbols
                .SingleOrDefault(sym => sym.DeclaringSyntax == syntax);
            if (symbol is UntypedLocalSymbol)
            {
                // NOTE: Special case, locals are untyped, we need the typed variant
                symbol = this.symbolMap[syntax];
            }
            return symbol?.ToApiSymbol();
        }
        case SourceModuleSymbol module:
        {
            // Just search for the corresponding syntax
            var symbol = module.Members
                .SingleOrDefault(sym => sym.DeclaringSyntax == syntax);
            return symbol?.ToApiSymbol();
        }
        default:
            throw new NotImplementedException();
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
        if (this.symbolMap.TryGetValue(syntax, out var existing)) return existing?.ToApiSymbol();

        void BindEnclosing()
        {
            var binder = this.GetBinder(syntax);
            var containingSymbol = binder.ContainingSymbol;
            switch (containingSymbol)
            {
            default:
                throw new NotImplementedException();
            }
        }

        switch (syntax)
        {
        case MemberExpressionSyntax:
        {
            // Bind the enclosing entity
            BindEnclosing();
            return this.symbolMap[syntax].ToApiSymbol();
        }
        default:
            // TODO
            throw new NotImplementedException();
        }
    }

    private Binder GetBinder(SyntaxNode syntax)
    {
        var binder = this.compilation.GetBinder(syntax);
        return new IncrementalBinder(binder, this);
    }

    private Binder GetBinder(Symbol symbol)
    {
        var binder = this.compilation.GetBinder(symbol);
        return new IncrementalBinder(binder, this);
    }
}
