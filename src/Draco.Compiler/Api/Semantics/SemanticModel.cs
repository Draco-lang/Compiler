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

    internal DiagnosticBag DiagnosticBag { get; } = new();
    DiagnosticBag IBinderProvider.DiagnosticBag => this.DiagnosticBag;

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
        var syntaxNodes = span is null
            ? this.Tree.PreOrderTraverse()
            : this.Tree.TraverseSubtreesIntersectingSpan(span.Value);

        var addedImportBinders = new HashSet<ImportBinder>();

        foreach (var syntaxNode in syntaxNodes)
        {
            // Add syntax diagnostics
            var syntaxDiagnostics = this.Tree.SyntaxDiagnosticTable.Get(syntaxNode);
            this.DiagnosticBag.AddRange(syntaxDiagnostics);

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
                containingSymbol?.Bind(this);
                break;
            }
            // NOTE: Only globals need binding
            case VariableDeclarationSyntax when containingSymbol is SourceModuleSymbol containingModule:
            {
                // We need to search for this global
                var globalSymbol = containingModule.Members
                    .OfType<SourceGlobalSymbol>()
                    .Single(s => s.DeclaringSyntax == syntaxNode);
                globalSymbol.Bind(this);
                break;
            }
            case ImportDeclarationSyntax:
            {
                // We get the binder, and if this binder wasn't added yet, we add its import errors
                var importBinder = this.GetImportBinder(syntaxNode);
                if (addedImportBinders.Add(importBinder))
                {
                    // New binder, add errors
                    // First, enforce binding
                    _ = importBinder.ImportItems;
                    this.DiagnosticBag.AddRange(importBinder.ImportDiagnostics);
                }
                break;
            }
            }
        }

        return this.DiagnosticBag.ToImmutableArray();
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
            func.Bind(this);

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
        if (syntax is ImportPathSyntax)
        {
            // Imports are special, we need to search in the binder
            var importBinder = this.GetImportBinder(syntax);
            var importItems = importBinder.ImportItems;
            var importSyntax = GetImportSyntax(syntax);
            // Search for the import item
            var importItem = importItems.SingleOrDefault(i => i.Syntax == importSyntax);
            // Not found in item
            if (importItem is null) return null;
            // Search for path element
            // NOTE: Yes, this could be simplified to a SingleOrDefault to a predicate,
            // but the default for a KeyValuePair is not nullable, so a null-warn would be swallowed
            // Decided to write it this way, in case the code gets shuffled around later
            var pathSymbol = importItem.Path
                .Where(i => i.Key == syntax)
                .Select(i => i.Value)
                .SingleOrDefault();
            // Not found in path
            if (pathSymbol is null) return null;
            return pathSymbol.ToApiSymbol();
        }

        if (this.symbolMap.TryGetValue(syntax, out var existing)) return existing.ToApiSymbol();

        // Get enclosing context
        var binder = this.GetBinder(syntax);
        var containingSymbol = binder.ContainingSymbol;

        switch (containingSymbol)
        {
        case SourceFunctionSymbol func:
        {
            // Bind the function contents
            func.Bind(this);
            break;
        }
        case SourceModuleSymbol module:
        {
            // Bind top-level members
            foreach (var member in module.Members.OfType<ISourceSymbol>())
            {
                member.Bind(this);
            }
            break;
        }
        }

        // Attempt to retrieve
        this.symbolMap.TryGetValue(syntax, out var symbol);
        return symbol?.ToApiSymbol();
    }

    private ImportBinder GetImportBinder(SyntaxNode syntax)
    {
        var binder = this.compilation.GetBinder(syntax);
        while (true)
        {
            if (binder is ImportBinder importBinder) return importBinder;
            binder = binder.Parent!;
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

    Binder IBinderProvider.GetBinder(SyntaxNode syntax) => this.GetBinder(syntax);
    Binder IBinderProvider.GetBinder(Symbol symbol) => this.GetBinder(symbol);

    private static ImportDeclarationSyntax GetImportSyntax(SyntaxNode syntax)
    {
        while (true)
        {
            if (syntax is ImportDeclarationSyntax decl) return decl;
            syntax = syntax.Parent!;
        }
    }
}
