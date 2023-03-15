using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds local variables.
///
/// A local binder is a bit more complex because of shadowing. When a lookup happens, the relative position of
/// things have to be considered. Example:
///
/// var x = 0; // x1
/// {
///     var y = x; // y1 that references x1
/// }
/// var x = x; // x2 that references x1
/// var y = x; // y2 that references x2
/// </summary>
internal sealed class LocalBinder : Binder
{
    public readonly record struct LocalDeclaration(int Position, Symbol Symbol);

    private ImmutableDictionary<SyntaxNode, int> RelativePositions
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.relativePositions!;
        }
    }

    /// <summary>
    /// The position-independent symbols declared in this scope.
    /// </summary>
    public ImmutableArray<Symbol> Declarations
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.declarations;
        }
    }

    /// <summary>
    /// The locals (position-dependent symbols) declared in this scope.
    /// </summary>
    public ImmutableArray<LocalDeclaration> LocalDeclarations
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.localDeclarations;
        }
    }

    public override IEnumerable<Symbol> DeclaredSymbols => this.Declarations
        .Concat(this.LocalDeclarations.Select(d => d.Symbol));

    private bool NeedsBuild => this.relativePositions is null;

    private ImmutableDictionary<SyntaxNode, int>? relativePositions;
    private ImmutableArray<Symbol> declarations;
    private ImmutableArray<LocalDeclaration> localDeclarations;

    private readonly SyntaxNode syntax;

    public LocalBinder(Binder parent, SyntaxNode syntax)
        : base(parent)
    {
        this.syntax = syntax;
    }

    public override void LookupValueSymbol(LookupResult result, string name, SyntaxNode? reference)
    {
        // If there's a syntactic reference, check locals
        if (reference is not null)
        {
            // Only check the ones that come before the reference position
            var position = this.RelativePositions[reference];
            var localSymbol = this.LocalDeclarations
                .Where(decl => decl.Position < position && decl.Symbol.Name == name)
                .Select(decl => decl.Symbol)
                .LastOrDefault();
            // If there was a symbol, we can reference it
            if (localSymbol is not null) result.Add(localSymbol);
        }
        // Now we can check order-independent declarations
        foreach (var decl in this.Declarations)
        {
            if (decl.Name != name) continue;
            if (!BinderFacts.IsValueSymbol(decl)) continue;
            result.Add(decl);
        }
        // If we are collecting an overload-set or the result is empty, we try to continue upwards
        // Otherwise we can stop
        if (!result.FoundAny || result.IsOverloadSet)
        {
            var parentReference = BinderFacts.GetScopeDefiningAncestor(reference?.Parent);
            this.Parent?.LookupValueSymbol(result, name, parentReference);
        }
    }

    public override void LookupTypeSymbol(LookupResult result, string name, SyntaxNode? reference)
    {
        foreach (var decl in this.Declarations)
        {
            if (decl.Name != name) continue;
            if (!BinderFacts.IsTypeSymbol(decl)) continue;
            result.Add(decl);
        }
        if (!result.FoundAny) this.Parent?.LookupTypeSymbol(result, name, reference);
    }

    private void Build()
    {
        var relativePositionsBuilder = ImmutableDictionary.CreateBuilder<SyntaxNode, int>();
        var declarationsBuilder = ImmutableArray.CreateBuilder<Symbol>();
        var localDeclarationsBuilder = ImmutableArray.CreateBuilder<LocalDeclaration>();
        var position = 0;
        foreach (var syntax in EnumerateNodesInSameScope(this.syntax))
        {
            // We skip tokens, those are cached
            if (syntax is SyntaxToken) continue;
            // First off, we add to the position translator
            relativePositionsBuilder.Add(syntax, position);
            // Next, we check if the syntax defines some kind of symbol
            var symbol = this.BuildSymbol(syntax);
            if (symbol is not null)
            {
                // There is a symbol being built
                // If it's a local, it depends on position, otherwise we don't care
                if (symbol is LocalSymbol) localDeclarationsBuilder.Add(new(position, symbol));
                else declarationsBuilder.Add(symbol);
            }
            // Increment relative position
            ++position;
        }
        this.relativePositions = relativePositionsBuilder.ToImmutable();
        this.declarations = declarationsBuilder.ToImmutable();
        this.localDeclarations = localDeclarationsBuilder.ToImmutable();
    }

    private Symbol? BuildSymbol(SyntaxNode syntax) => syntax switch
    {
        FunctionDeclarationSyntax function => new SourceFunctionSymbol(this.ContainingSymbol, function),
        ParameterSyntax parameter => new SourceParameterSymbol(this.ContainingSymbol, parameter),
        VariableDeclarationSyntax variable => new SourceLocalSymbol(this.ContainingSymbol, variable),
        LabelDeclarationSyntax label => new SourceLabelSymbol(this.ContainingSymbol, label),
        _ => null,
    };

    private static IEnumerable<SyntaxNode> EnumerateNodesInSameScope(SyntaxNode tree)
    {
        // We go through each child of the current tree
        foreach (var child in tree.Children)
        {
            // We yield the child first
            yield return child;

            // If the child defines a scope, we don't recurse
            if (BinderFacts.DefinesScope(child)) continue;

            // Otherwise, we can recurse
            foreach (var item in EnumerateNodesInSameScope(child)) yield return item;
        }
    }
}
