using System.Collections.Generic;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Represents a single scope that binds the syntax-tree to the untyped-tree and then the bound-tree.
/// </summary>
internal abstract partial class Binder(Compilation compilation, Binder? parent)
{
    /// <summary>
    /// The compilation this binder was created for.
    /// </summary>
    internal Compilation Compilation { get; } = compilation;

    /// <summary>
    /// Utility accessor for intrinsics.
    /// </summary>
    protected WellKnownTypes WellKnownTypes => this.Compilation.WellKnownTypes;

    /// <summary>
    /// The parent binder of this one.
    /// </summary>
    internal Binder? Parent { get; } = parent;

    /// <summary>
    /// The ancestor chain of this binder, starting from this binder and going up to the root binder.
    /// </summary>
    internal IEnumerable<Binder> AncestorChain
    {
        get
        {
            var current = this;
            while (current is not null)
            {
                yield return current;
                current = current.Parent;
            }
        }
    }

    /// <summary>
    /// The syntax that constructed this binder.
    /// </summary>
    public virtual SyntaxNode? DeclaringSyntax => this.Parent?.DeclaringSyntax;

    /// <summary>
    /// The symbol containing the binding context.
    /// </summary>
    public virtual Symbol? ContainingSymbol => this.Parent?.ContainingSymbol;

    /// <summary>
    /// The symbols declared in this binder scope.
    /// </summary>
    public virtual IEnumerable<Symbol> DeclaredSymbols => [];

    protected Binder(Compilation compilation)
        : this(compilation, null)
    {
    }

    protected Binder(Binder parent)
        : this(parent.Compilation, parent)
    {
    }

    /// <summary>
    /// Retrieves the appropriate binder for the given syntax node.
    /// </summary>
    /// <param name="node">The node to retrieve the binder for.</param>
    /// <returns>The appropriate binder for the node.</returns>
    protected virtual Binder GetBinder(SyntaxNode node) =>
        this.Compilation.GetBinder(node);

    // NOTE: This is a hack, until we find out something nicer
    // We essentially use this to notify incremental binder that a left-hand side of a module or a type access
    // will be erased, won't be present in the bound tree.
    // Once we start modeling module member access without throwing it away, we can get rid of it.
    // In addition, this is used by for-loops too to associate the iterator with its symbol
    internal virtual void BindSyntaxToSymbol(SyntaxNode syntax, Symbol module) { }
    internal virtual void BindTypeSyntaxToSymbol(SyntaxNode syntax, TypeSymbol type) { }

    private static FunctionSymbol GetGetterSymbol(SyntaxNode? syntax, PropertySymbol prop, DiagnosticBag diags)
    {
        var result = prop.Getter;
        if (result is null)
        {
            diags.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.CannotGetSetOnlyProperty,
                location: syntax?.Location,
                prop.FullName));
            result = new ErrorPropertyAccessorSymbol(prop);
        }
        return result;
    }

    private static FunctionSymbol GetSetterSymbol(SyntaxNode? syntax, PropertySymbol prop, DiagnosticBag diags)
    {
        var result = prop.Setter;
        if (result is null)
        {
            diags.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.CannotSetGetOnlyProperty,
                location: syntax?.Location,
                prop.FullName));
            result = new ErrorPropertyAccessorSymbol(prop);
        }
        return result;
    }
}
