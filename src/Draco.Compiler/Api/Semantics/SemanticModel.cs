using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;

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
    public ImmutableArray<Diagnostic> Diagnostics =>
        InterlockedUtils.InitializeDefault(ref this.diagnostics, () => this.GetDiagnostics());
    private ImmutableArray<Diagnostic> diagnostics;

    internal DiagnosticBag DiagnosticBag { get; } = new();
    DiagnosticBag IBinderProvider.DiagnosticBag => this.DiagnosticBag;

    private readonly Compilation compilation;

    // Filled out by incremental binding
    private readonly ConcurrentDictionary<SourceFunctionSymbol, BoundStatement> boundFunctions = new();
    private readonly ConcurrentDictionary<SourceGlobalSymbol, (Internal.Symbols.TypeSymbol Type, BoundExpression? Value)> boundGlobals = new();
    private readonly ConcurrentDictionary<(SyntaxNode, System.Type), BoundNode> boundNodeMap = new();
    private readonly ConcurrentDictionary<SyntaxNode, Symbol> symbolMap = new();

    internal SemanticModel(Compilation compilation, SyntaxTree tree)
    {
        this.Tree = tree;
        this.compilation = compilation;
    }

    /// <summary>
    /// Retrieves all <see cref="Diagnostic"/>s on <see cref="Tree"/>.
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

    /// <summary>
    /// Retrieves all <see cref="ISymbol"/>s accesible from given <paramref name="node"/>.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> from which to start looking for declared symbols.</param>
    /// <returns>All the <see cref="ISymbol"/>s accesible from the <paramref name="node"/>.</returns>
    public ImmutableArray<ISymbol> GetAllDefinedSymbols(SyntaxNode node)
    {
        var result = new HashSet<ISymbol>();
        var binder = this.compilation.GetBinder(node);
        while (binder is not null)
        {
            var symbols = binder.DeclaredSymbols
                .Select(x => x is UntypedLocalSymbol loc ? this.GetDeclaredSymbol(loc.DeclaringSyntax)! : x.ToApiSymbol());
            foreach (var s in symbols) result.Add(s);
            binder = binder.Parent;
        }
        return result.ToImmutableArray();
    }

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

            // Could be a generic parameter
            if (syntax is GenericParameterSyntax genericParam)
            {
                var paramSymbol = containingSymbol.GenericParameters
                    .FirstOrDefault(p => p.DeclaringSyntax == syntax);
                return paramSymbol?.ToApiSymbol();
            }

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
            // The module itself
            if (module.DeclaringSyntaxes.Contains(syntax)) return containingSymbol.ToApiSymbol();

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

    /// <summary>
    /// Retrieves the type of the expression represented by <paramref name="syntax"/>.
    /// </summary>
    /// <param name="syntax">The expression that the type will be checked of.</param>
    /// <returns>The <see cref="ITypeSymbol"/> that <paramref name="syntax"/> will evaluate to,
    /// or null if it does not evaluate to a value with type.</returns>
    public ITypeSymbol? TypeOf(ExpressionSyntax syntax)
    {
        if (this.TryGetBoundNode(syntax, typeof(BoundExpression), out var existing))
        {
            return (existing as BoundExpression)?.Type?.ToApiSymbol();
        }

        // NOTE: Very similar logic to GetReferencedSymbol, maybe factor out?
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
        // TODO: We are passing in the wrong node type here...
        this.TryGetBoundNode(syntax, typeof(BoundExpression), out var node);
        return (node as BoundExpression)?.Type?.ToApiSymbol();
    }

    private bool TryGetBoundNode(SyntaxNode syntax, System.Type type, [MaybeNullWhen(false)] out BoundNode node) =>
        this.boundNodeMap.TryGetValue((syntax, type), out node);

    /// <summary>
    /// Retrieves the function overloads referenced by <paramref name="syntax"/>.
    /// </summary>
    /// <param name="syntax">The tree that is asked for the referenced overloads.</param>
    /// <returns>The referenced overloads by <paramref name="syntax"/>, or empty array
    /// if it does not reference any.</returns>
    public ImmutableArray<ISymbol> GetReferencedOverloads(ExpressionSyntax syntax)
    {
        if (syntax is MemberExpressionSyntax member)
        {
            var symbol = this.TypeOf(member.Accessed) ?? this.GetReferencedSymbol(member.Accessed);
            if (symbol is null) return ImmutableArray<ISymbol>.Empty;
            else return symbol.Members.Where(x => x is FunctionSymbol && x.Name == member.Member.Text).ToImmutableArray();
        }
        // We look up syntax based on the symbol in context
        var binder = this.compilation.GetBinder(syntax);
        var result = new HashSet<ISymbol>();
        while (binder is not null)
        {
            var symbols = binder.DeclaredSymbols
                .Select(x => x is UntypedLocalSymbol loc
                    ? this.GetDeclaredSymbol(loc.DeclaringSyntax)!
                    : x.ToApiSymbol())
                .Where(x => x is FunctionSymbol && x.Name == syntax.ToString());
            foreach (var s in symbols) result.Add(s);
            binder = binder.Parent;
        }
        return result.ToImmutableArray();
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
