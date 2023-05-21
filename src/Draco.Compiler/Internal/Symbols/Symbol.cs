using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Base for all symbols within the language.
/// </summary>
internal abstract partial class Symbol
{
    /// <summary>
    /// The <see cref="Compilation"/> that defines this symbol.
    /// </summary>
    public virtual Compilation? DeclaringCompilation => this.ContainingSymbol?.DeclaringCompilation;

    /// <summary>
    /// The symbol directly containing this one.
    /// </summary>
    public abstract Symbol? ContainingSymbol { get; }

    /// <summary>
    /// The ancestor chain of this symbol, starting with this one.
    /// </summary>
    public IEnumerable<Symbol> AncestorChain
    {
        get
        {
            for (var result = this; result is not null; result = result.ContainingSymbol) yield return result;
        }
    }

    /// <summary>
    /// The root of this hierarchy.
    /// </summary>
    public Symbol? RootSymbol => this.AncestorChain.LastOrDefault();

    /// <summary>
    /// The root module of this hierarchy.
    /// </summary>
    public ModuleSymbol? RootModule => this.RootSymbol as ModuleSymbol;

    /// <summary>
    /// True, if this symbol represents some error.
    /// </summary>
    public virtual bool IsError => false;

    /// <summary>
    /// True, if this symbol represents special symbol.
    /// </summary>
    public virtual bool IsSpecialName => false;

    /// <summary>
    /// The name of this symbol.
    /// </summary>
    public virtual string Name => string.Empty;

    /// <summary>
    /// The fully qualified name of this symbol.
    /// </summary>
    public virtual string FullName
    {
        get
        {
            var parentFullName = this.ContainingSymbol?.FullName;
            return string.IsNullOrWhiteSpace(parentFullName)
                ? this.Name
                : $"{parentFullName}.{this.Name}";
        }
    }

    /// <summary>
    /// All the members within this symbol.
    /// </summary>
    public virtual IEnumerable<Symbol> Members => Enumerable.Empty<Symbol>();

    /// <summary>
    /// The static members within this symbol.
    /// </summary>
    public virtual IEnumerable<Symbol> StaticMembers => this.Members.Where(x => x is not ITypedSymbol typed || typed.IsStatic);

    /// <summary>
    /// The instance members within this symbol.
    /// </summary>
    public virtual IEnumerable<Symbol> InstanceMembers => this.Members.Where(x => x is ITypedSymbol typed && !typed.IsStatic);

    /// <summary>
    /// Documentation attached to this symbol.
    /// </summary>
    public virtual string Documentation => string.Empty;

    /// <summary>
    /// The visibility of this symbol.
    /// </summary>
    public virtual Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Internal;

    /// <summary>
    /// The syntax declaring this symbol.
    /// </summary>
    public virtual SyntaxNode? DeclaringSyntax => null;

    protected private static Api.Semantics.Visibility GetVisibilityFromTokenKind(TokenKind? kind) => kind switch
    {
        null => Api.Semantics.Visibility.Private,
        TokenKind.KeywordInternal => Api.Semantics.Visibility.Internal,
        TokenKind.KeywordPublic => Api.Semantics.Visibility.Public,
        _ => throw new System.InvalidOperationException($"illegal visibility modifier token {kind}"),
    };

    /// <summary>
    /// Converts this symbol into an API symbol.
    /// </summary>
    /// <returns>The equivalent API symbol.</returns>
    public abstract Api.Semantics.ISymbol ToApiSymbol();

    public abstract void Accept(SymbolVisitor visitor);
    public abstract TResult Accept<TResult>(SymbolVisitor<TResult> visitor);

    /// <summary>
    /// Converts the symbol-tree to a DOT graph for debugging purposes.
    /// </summary>
    /// <returns>The DOT graph of the symbol-tree.</returns>
    public string ToDot()
    {
        var builder = new DotGraphBuilder<Symbol>(isDirected: true);
        builder.WithName("SymbolTree");

        void Recurse(Symbol symbol)
        {
            builder!
                .AddVertex(symbol)
                .WithLabel($"{symbol.GetType().Name}\n{symbol}");
            foreach (var m in symbol.Members)
            {
                builder.AddEdge(symbol, m);
                Recurse(m);
            }
        }

        Recurse(this);

        return builder.ToDot();
    }

    public override string ToString() => this is ITypedSymbol typed
        ? $"{this.Name}: {typed.Type}"
        : this.Name;
}
