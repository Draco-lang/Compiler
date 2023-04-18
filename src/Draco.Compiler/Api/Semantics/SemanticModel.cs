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
public sealed partial class SemanticModel : IBinderProvider
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

        // Add global diagnostics
        diagnostics.AddRange(this.compilation.GlobalDiagnosticBag);

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
                containingSymbol?.Bind(this, diagnostics);
                break;
            }
            // NOTE: Only globals need binding
            case VariableDeclarationSyntax when containingSymbol is SourceModuleSymbol containingModule:
            {
                // We need to search for this global
                var globalSymbol = containingModule.Members
                    .OfType<SourceGlobalSymbol>()
                    .Single(s => s.DeclaringSyntax == syntaxNode);
                globalSymbol.Bind(this, diagnostics);
                break;
            }
            case ImportDeclarationSyntax:
            {
                // TODO: We are escaping memoization, this is AWFUL
                // Perform binding
                while (binder is not ImportBinder) binder = binder.Parent!;
                _ = binder.DeclaredSymbols;
                break;
            }
            }
        }

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
        if (this.symbolMap.TryGetValue(syntax, out var existing)) return existing.ToApiSymbol();

        // Get enclosing context
        var binder = this.GetBinder(syntax);
        var containingSymbol = binder.ContainingSymbol;

        switch (containingSymbol)
        {
        case SourceFunctionSymbol func:
        {
            // This is just the function itself
            if (func.DeclaringSyntax == syntax) return containingSymbol.ToApiSymbol();

            // Bind the function contents
            func.Bind(this, this.compilation.GlobalDiagnosticBag);

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
        if (this.symbolMap.TryGetValue(syntax, out var existing)) return existing.ToApiSymbol();

        // Get enclosing context
        var binder = this.GetBinder(syntax);
        var containingSymbol = binder.ContainingSymbol;

        switch (containingSymbol)
        {
        case SourceFunctionSymbol func:
        {
            // Bind the function contents
            func.Bind(this, this.compilation.GlobalDiagnosticBag);
            break;
        }
        case SourceModuleSymbol module:
        {
            // Bind top-level members
            foreach (var member in module.Members.OfType<ISourceSymbol>())
            {
                member.Bind(this, this.compilation.GlobalDiagnosticBag);
            }
            break;
        }
        default:
            throw new NotImplementedException();
        }

        // Attempt to retrieve
        this.symbolMap.TryGetValue(syntax, out var symbol);
        return symbol?.ToApiSymbol();
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

    Binder IBinderProvider.GetBinder(SyntaxNode syntax) => this.GetBinder(syntax);
    Binder IBinderProvider.GetBinder(Symbol symbol) => this.GetBinder(symbol);
}
