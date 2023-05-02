using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols.Generic;
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
    /// The ancestory chain of this symbol, starting with this one.
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
    /// True, if this is a generic definition.
    /// </summary>
    public bool IsGenericDefinition => this.GenericParameters.Length > 0;

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
            var thisFullName = string.IsNullOrWhiteSpace(parentFullName)
                ? this.Name
                : $"{parentFullName}.{this.Name}";
            if (this.GenericParameters.Length > 0) return $"{thisFullName}<{string.Join(", ", this.GenericParameters)}>";
            if (this.GenericArguments.Length > 0) return $"{thisFullName}<{string.Join(", ", this.GenericArguments)}>";
            return thisFullName;
        }
    }

    /// <summary>
    /// All the members within this symbol.
    /// </summary>
    public virtual IEnumerable<Symbol> Members => Enumerable.Empty<Symbol>();

    /// <summary>
    /// Documentation attached to this symbol.
    /// </summary>
    public virtual string Documentation => string.Empty;

    /// <summary>
    /// The syntax declaring this symbol.
    /// </summary>
    public virtual SyntaxNode? DeclaringSyntax => null;

    /// <summary>
    /// The generic definition of this symbol, in case this is a generic instance.
    /// </summary>
    public virtual Symbol? GenericDefinition => null;

    /// <summary>
    /// The generic parameters of this symbol.
    /// </summary>
    public virtual ImmutableArray<TypeParameterSymbol> GenericParameters => ImmutableArray<TypeParameterSymbol>.Empty;

    /// <summary>
    /// The generic arguments that this symbol was instantiated with.
    /// </summary>
    public virtual ImmutableArray<TypeSymbol> GenericArguments => ImmutableArray<TypeSymbol>.Empty;

    /// <summary>
    /// Instantiates this generic symbol with the given substitutions.
    /// </summary>
    /// <param name="containingSymbol">The symbol that should be considered the containing symbol.</param>
    /// <param name="context">The generic context.</param>
    /// <returns>This symbol with all type parameters replaced according to <paramref name="context"/>.</returns>
    public virtual Symbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        throw new System.NotSupportedException();

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
