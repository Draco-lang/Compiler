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
    public ImmutableArray<Diagnostic> Diagnostics => this.diagnostics ??= this.GetDiagnostics();
    private ImmutableArray<Diagnostic>? diagnostics;

    private readonly Compilation compilation;
    private readonly Dictionary<SyntaxNode, IList<BoundNode>> syntaxMap = new();
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

        // Add all syntax errors

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

        // TODO
        throw new NotImplementedException();
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
        // TODO
        throw new NotImplementedException();
    }

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> referenced by <paramref name="syntax"/>.
    /// </summary>
    /// <param name="syntax">The tree that is asked for the referenced <see cref="ISymbol"/>.</param>
    /// <returns>The referenced <see cref="ISymbol"/> by <paramref name="syntax"/>, or null
    /// if it does not reference any.</returns>
    public ISymbol? GetReferencedSymbol(SyntaxNode syntax)
    {
        // TODO
        throw new NotImplementedException();
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
