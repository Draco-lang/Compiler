using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Symbols.Generic;
using Draco.Compiler.Internal.Symbols.Metadata;
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
    /// True, if this is a generic definition.
    /// </summary>
    public bool IsGenericDefinition => this.GenericParameters.Length > 0;

    /// <summary>
    /// True, if this is a generic instantiated symbol.
    /// </summary>
    public bool IsGenericInstance => this.GenericArguments.Length > 0;

    /// <summary>
    /// The metadata name of this symbol.
    /// </summary>
    public virtual string MetadataName => this.Name;

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
            return $"{thisFullName}{this.GenericsToString()}";
        }
    }

    /// <summary>
    /// The fully qualified metadata name of this symbol.
    /// </summary>
    public virtual string MetadataFullName
    {
        get
        {
            var parentFullName = this.ContainingSymbol is not MetadataAssemblySymbol
                ? this.ContainingSymbol?.MetadataFullName
                : null;

            return string.IsNullOrWhiteSpace(parentFullName)
                ? this.MetadataName
                : $"{parentFullName}.{this.MetadataName}";
        }
    }

    /// <summary>
    /// All the members within this symbol.
    /// </summary>
    public virtual IEnumerable<Symbol> Members => Enumerable.Empty<Symbol>();

    /// <summary>
    /// The static members within this symbol.
    /// </summary>
    public virtual IEnumerable<Symbol> StaticMembers => this.Members.Where(x => x is IMemberSymbol mem && mem.IsStatic);

    /// <summary>
    /// The instance members within this symbol.
    /// </summary>
    public virtual IEnumerable<Symbol> InstanceMembers => this.Members.Where(x => x is IMemberSymbol mem && !mem.IsStatic);

    /// <summary>
    /// The structured documentation attached to this symbol.
    /// </summary>
    public virtual SymbolDocumentation Documentation => SymbolDocumentation.Empty;

    /// <summary>
    /// The documentation of symbol as raw xml or markdown;
    /// </summary>
    internal virtual string RawDocumentation => string.Empty;

    /// <summary>
    /// The visibility of this symbol.
    /// </summary>
    public virtual Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Internal;

    /// <summary>
    /// The syntax declaring this symbol.
    /// </summary>
    public virtual SyntaxNode? DeclaringSyntax => null;

    private protected static Api.Semantics.Visibility GetVisibilityFromTokenKind(TokenKind? kind) => kind switch
    {
        null => Api.Semantics.Visibility.Private,
        TokenKind.KeywordInternal => Api.Semantics.Visibility.Internal,
        TokenKind.KeywordPublic => Api.Semantics.Visibility.Public,
        _ => throw new System.InvalidOperationException($"illegal visibility modifier token {kind}"),
    };

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
    /// Checks if this symbol can be shadowed by <paramref name="other"/> symbol.
    /// </summary>
    /// <param name="other">The other symbol.</param>
    /// <returns>True, if this symbol can be shadowed by <paramref name="other"/> symbol,
    /// otherwise false.</returns>
    public virtual bool CanBeShadowedBy(Symbol other) => this.Name == other.Name;

    /// <summary>
    /// Instantiates this generic symbol with the given substitutions.
    /// </summary>
    /// <param name="containingSymbol">The symbol that should be considered the containing symbol.</param>
    /// <param name="arguments">The generic arguments.</param>
    /// <returns>The instantiated symbol.</returns>
    public virtual Symbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments)
    {
        if (this.GenericParameters.Length != arguments.Length)
        {
            throw new System.ArgumentException(
                $"the number of generic parameters ({this.GenericParameters.Length}) does not match the passed in number of arguments ({arguments.Length})",
                nameof(arguments));
        }

        var substitutions = this.GenericParameters
            .Zip(arguments)
            .ToImmutableDictionary(pair => pair.First, pair => pair.Second);
        var context = new GenericContext(substitutions);
        return this.GenericInstantiate(containingSymbol, context);
    }

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

    /// <summary>
    /// Turns the generic list of this symbol into a string representation.
    /// </summary>
    /// <returns>The generic parameters or arguments between angle brackets, or an empty string,
    /// if this symbol is not a generic definition or instantiation.</returns>
    public string GenericsToString()
    {
        if (this.IsGenericDefinition) return $"<{string.Join(", ", this.GenericParameters)}>";
        if (this.IsGenericInstance) return $"<{string.Join(", ", this.GenericArguments)}>";
        return string.Empty;
    }
}
