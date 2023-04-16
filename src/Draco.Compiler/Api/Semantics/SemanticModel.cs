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
        void CollectFunctionDiagnostics(SourceFunctionSymbol func, DiagnosticBag result)
        {
            _ = func.Parameters;
            _ = func.ReturnType;

            // Avoid double-evaluation of diagnostics
            if (!this.syntaxMap.ContainsKey(func.DeclarationSyntax.Body))
            {
                _ = func.Body;

                // Flow passes
                ReturnsOnAllPaths.Analyze(func, result);
                DefiniteAssignment.Analyze(func.Body, result);
                ValAssignment.Analyze(func, result);

                // Collect in locals
                var localFunctions = BoundTreeCollector.CollectLocalFunctions(func.Body);
                foreach (var localFunc in localFunctions.OfType<SourceFunctionSymbol>())
                {
                    CollectFunctionDiagnostics(localFunc, result);
                }
            }
        }

        void CollectGlobalDiagnostics(SourceGlobalSymbol global, DiagnosticBag result)
        {
            _ = global.Type;
            _ = global.Value;

            // Flow passes
            if (global.Value is not null)
            {
                DefiniteAssignment.Analyze(global.Value, result);
            }
            ValAssignment.Analyze(global, result);

            if (global.Value is not null)
            {
                // Collect in locals
                var localFunctions = BoundTreeCollector.CollectLocalFunctions(global.Value);
                foreach (var localFunc in localFunctions.OfType<SourceFunctionSymbol>())
                {
                    CollectFunctionDiagnostics(localFunc, result);
                }
            }
        }

        var result = new DiagnosticBag();

        // Retrieve all syntax errors
        var syntaxDiagnostics = this.compilation.SyntaxTrees.SelectMany(tree => tree.Diagnostics);
        result.AddRange(syntaxDiagnostics);

        // Enforce import bindings
        // TODO: This is an awful hack
        // Maybe we should introduce some "EnforceBinding(DiagnosticBag)" method for bindable things
        foreach (var import in this.Tree.PreOrderTraverse().OfType<ImportDeclarationSyntax>())
        {
            if (this.symbolMap.ContainsKey(import.Path)) continue;
            // TODO: We are escaping memoization, this is AWFUL
            var binder = this.compilation.GetBinder(import);
            while (binder is not ImportBinder) binder = binder.Parent!;
            _ = binder.DeclaredSymbols;
        }

        // Next, we enforce binding everywhere
        foreach (var symbol in this.compilation.SourceModule.Members)
        {
            if (symbol is SourceFunctionSymbol func)
            {
                CollectFunctionDiagnostics(func, result);
            }
            else if (symbol is SourceGlobalSymbol global)
            {
                CollectGlobalDiagnostics(global, result);
            }
        }

        // Dump back global diagnostics
        result.AddRange(this.compilation.GlobalDiagnosticBag);

        return result.ToImmutableArray();
    }

    /// <summary>
    /// Retrieves all <see cref="ISymbol"/>s accesible from given <paramref name="node"/>.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> from which to start looking for declared symbols.</param>
    /// <returns>All the <see cref="ISymbol"/>s accesible from the <paramref name="node"/>.</returns>
    public ImmutableArray<ISymbol> GetAllDefinedSymbols(SyntaxNode node)
    {
        var result = ImmutableArray.CreateBuilder<ISymbol>();
        var binder = this.compilation.GetBinder(node);
        while (binder is not null)
        {
            result.AddRange(binder.DeclaredSymbols.Select(x => x is UntypedLocalSymbol loc ? this.GetDefinedSymbol(loc.DeclarationSyntax)! : x.ToApiSymbol()));
            binder = binder.Parent;
        }
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
                var functionBinder = this.GetBinder(function);
                _ = functionBinder.BindFunction(function, diagnostics);
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
            var isInMap = this.symbolMap.ContainsKey(syntax) || this.syntaxMap.ContainsKey(syntax);
            if (!isInMap)
            {
                var diagnostics = this.compilation.GlobalDiagnosticBag;

                var functionBinder = this.GetBinder(function);
                _ = functionBinder.BindFunction(function, diagnostics);

                // TODO: This is not even close to correct, the functionBinder is likely not responsible
                // for binding this syntax
                // We should retrieve the proper binder
                if (syntax is ImportPathSyntax importPath)
                {
                    _ = functionBinder.BindImportPath(importPath, diagnostics);
                }

                // Since the parameter types and the return type are in this scope too, bind them
                var functionSyntax = function.DeclarationSyntax;
                // Parameters
                foreach (var param in functionSyntax.ParameterList.Values)
                {
                    _ = functionBinder.BindType(param.Type, diagnostics);
                }
                // Return type
                if (functionSyntax.ReturnType is not null)
                {
                    _ = functionBinder.BindType(functionSyntax.ReturnType.Type, diagnostics);
                }
            }

            // Now the syntax node should be in the map
            if (this.symbolMap.TryGetValue(syntax, out var symbol)) return symbol.ToApiSymbol();
            if (!this.syntaxMap.TryGetValue(syntax, out var boundNodes)) return null;
            // TODO: We need to deal with potential multiple returns here
            if (boundNodes.Count != 1) throw new NotImplementedException();
            // Just return the singleton symbol
            return ExtractReferencedSymbol(boundNodes[0])?.ToApiSymbol();
        }
        case SourceModuleSymbol module:
        {
            var isInMap = this.symbolMap.ContainsKey(syntax) || this.syntaxMap.ContainsKey(syntax);
            if (!isInMap)
            {
                // We don't have a choice, we need to go through top-level module elements
                // and bind everything incrementally
                var moduleBinder = this.GetBinder(this.Tree.Root);
                var diagnostics = this.compilation.GlobalDiagnosticBag;

                // TODO: This is not even close to correct, the moduleBinder is likely not responsible
                // for binding this syntax
                // We should retrieve the proper binder
                if (syntax is ImportPathSyntax importPath)
                {
                    _ = moduleBinder.BindImportPath(importPath, diagnostics);
                }

                foreach (var member in module.Members)
                {
                    switch (member)
                    {
                    case SourceGlobalSymbol global:
                    {
                        // Bind type and value
                        _ = moduleBinder.BindGlobal(global, diagnostics);
                        break;
                    }
                    // NOTE: Anything else to handle?
                    }
                }
            }

            // Now the syntax node should be in the appropriate map
            if (this.symbolMap.TryGetValue(syntax, out var symbol)) return symbol.ToApiSymbol();
            if (!this.syntaxMap.TryGetValue(syntax, out var boundNodes)) return null;
            // TODO: We need to deal with potential multiple returns here
            if (boundNodes.Count != 1) throw new NotImplementedException();
            // Just return the singleton symbol
            return ExtractReferencedSymbol(boundNodes[0])?.ToApiSymbol();
        }
        default:
            return null;
        }
    }

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
            var accessed = this.GetReferencedSymbol(member.Accessed);
            if (accessed is null) return ImmutableArray<ISymbol>.Empty;
            if (accessed is ITypedSymbol typed) return typed.Type.Members.Where(x => x is FunctionSymbol && x.Name == member.Member.Text).ToImmutableArray();
            else return accessed.Members.Where(x => x is FunctionSymbol && x.Name == member.Member.Text).ToImmutableArray();
        }
        // We look up syntax based on the symbol in context
        var binder = this.compilation.GetBinder(syntax);
        var result = ImmutableArray.CreateBuilder<ISymbol>();
        while (binder is not null)
        {
            result.AddRange(binder.DeclaredSymbols.Select(x => x is UntypedLocalSymbol loc ? this.GetDefinedSymbol(loc.DeclarationSyntax)! : x.ToApiSymbol()).Where(x => x is FunctionSymbol && x.Name == syntax.ToString()));
            binder = binder.Parent;
        }
        return result.ToImmutable();
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
        BoundMemberExpression m => m.Member,
        BoundIllegalLvalue => null,
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };
}
