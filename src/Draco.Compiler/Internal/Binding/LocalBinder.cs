using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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
            if (!this.NeedsBuild) return this.relativePositions!;
            lock (this.buildLock)
            {
                if (this.NeedsBuild) this.Build();
                return this.relativePositions!;
            }
        }
    }

    /// <summary>
    /// The position-independent symbols declared in this scope.
    /// </summary>
    public ImmutableArray<Symbol> Declarations
    {
        get
        {
            if (!this.NeedsBuild) return this.declarations;
            lock (this.buildLock)
            {
                if (this.NeedsBuild) this.Build();
                return this.declarations;
            }
        }
    }

    /// <summary>
    /// The locals (position-dependent symbols) declared in this scope.
    /// </summary>
    public ImmutableArray<LocalDeclaration> LocalDeclarations
    {
        get
        {
            if (!this.NeedsBuild) return this.localDeclarations;
            lock (this.buildLock)
            {
                if (this.NeedsBuild) this.Build();
                return this.localDeclarations;
            }
        }
    }

    public override SyntaxNode DeclaringSyntax { get; }

    public override IEnumerable<Symbol> DeclaredSymbols => this.Declarations
        .Concat(this.LocalDeclarations.Select(d => d.Symbol));

    public override Symbol ContainingSymbol => base.ContainingSymbol ?? throw new InvalidOperationException();

    // IMPORTANT: The choice of flag field is important because of write order
    private bool NeedsBuild => Volatile.Read(ref this.relativePositions) is null;

    private readonly object buildLock = new();

    private ImmutableDictionary<SyntaxNode, int>? relativePositions;
    private ImmutableArray<Symbol> declarations;
    private ImmutableArray<LocalDeclaration> localDeclarations;

    public LocalBinder(Binder parent, SyntaxNode syntax)
        : base(parent)
    {
        if (!BinderFacts.DefinesScope(syntax))
        {
            throw new ArgumentException("the bound syntax must define a scope for local binding", nameof(syntax));
        }
        this.DeclaringSyntax = syntax;
    }

    internal override void LookupLocal(LookupResult result, string name, ref LookupFlags flags, Predicate<Symbol> allowSymbol, SyntaxNode? currentReference)
    {
        // NOTE: In case there is a local later, we could add it, but if it ends up being referenced,
        // we should log a diagnostic message
        // Maybe we can utilize the lookup result storing multiple symbols this way as well

        // If there's a syntactic reference, and we allow for locals check locals
        if (!flags.HasFlag(LookupFlags.DisallowLocals)
            && currentReference is not null
            && this.RelativePositions.TryGetValue(currentReference, out var position))
        {
            // Only check the ones that come before the reference position
            var localSymbol = this.LocalDeclarations
                .Where(decl => allowSymbol(decl.Symbol))
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
            if (!allowSymbol(decl)) continue;
            result.Add(decl);
        }
    }

    private void Build()
    {
        var relativePositionsBuilder = ImmutableDictionary.CreateBuilder<SyntaxNode, int>();
        var declarationsBuilder = ImmutableArray.CreateBuilder<Symbol>();
        var localDeclarationsBuilder = ImmutableArray.CreateBuilder<LocalDeclaration>();
        var position = 0;
        var localCount = 0;
        foreach (var syntax in EnumerateNodesInSameScope(this.DeclaringSyntax))
        {
            // First off, we add to the position translator
            relativePositionsBuilder.Add(syntax, position);
            // Next, we check if the syntax defines some kind of symbol
            var symbol = this.BuildSymbol(syntax, localCount);
            if (symbol is not null)
            {
                // There is a symbol being built
                // If it's a local, it depends on position, otherwise we don't care
                if (symbol is LocalSymbol)
                {
                    // Locals need to be offset by their width
                    var width = EnumerateNodesInSameScope(syntax).Count();
                    localDeclarationsBuilder.Add(new(position + width, symbol));
                    ++localCount;
                }
                else
                {
                    declarationsBuilder.Add(symbol);
                }
            }
            // Increment relative position
            ++position;
        }
        this.declarations = declarationsBuilder.ToImmutable();
        this.localDeclarations = localDeclarationsBuilder.ToImmutable();
        // IMPORTANT: relativePositions is used as the build flag, it has to be set last
        Volatile.Write(ref this.relativePositions, relativePositionsBuilder.ToImmutable());
    }

    private Symbol? BuildSymbol(SyntaxNode syntax, int localCount) => syntax switch
    {
        FunctionDeclarationSyntax function => new SourceFunctionSymbol(this.ContainingSymbol, function),
        ParameterSyntax parameter => new SourceParameterSymbol(this.ContainingSymbol, parameter),
        VariableDeclarationSyntax variable => new SourceLocalSymbol(this.ContainingSymbol, new TypeVariable(localCount), variable),
        LabelDeclarationSyntax label => new SourceLabelSymbol(this.ContainingSymbol, label),
        _ => null,
    };

    private static IEnumerable<SyntaxNode> EnumerateNodesInSameScope(SyntaxNode tree) =>
        BinderFacts.EnumerateNodesInSameScope(tree).Where(tree => tree is not SyntaxToken);
}
