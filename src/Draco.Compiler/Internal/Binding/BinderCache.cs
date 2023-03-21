using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Responsible for caching the binders for syntax nodes and declarations.
/// </summary>
internal sealed class BinderCache
{
    private readonly Compilation compilation;
    private readonly Dictionary<SyntaxNode, Binder> binders = new();

    public BinderCache(Compilation compilation)
    {
        this.compilation = compilation;
    }

    /// <summary>
    /// Retrieves a <see cref="Binder"/> for the given syntax node.
    /// </summary>
    /// <param name="syntax">The syntax node to retrieve the binder for.</param>
    /// <returns>The binder for <paramref name="syntax"/>.</returns>
    public Binder GetBinder(SyntaxNode syntax)
    {
        var scopeDefiningAncestor = BinderFacts.GetScopeDefiningAncestor(syntax);
        Debug.Assert(scopeDefiningAncestor is not null);

        if (!this.binders.TryGetValue(scopeDefiningAncestor, out var binder))
        {
            binder = this.BuildBinder(scopeDefiningAncestor);
            this.binders.Add(scopeDefiningAncestor, binder);
        }

        return binder;
    }

    private Binder BuildBinder(SyntaxNode syntax) => syntax switch
    {
        CompilationUnitSyntax => this.BuildCompilationUnitBinder(),
        FunctionDeclarationSyntax decl => this.BuildFunctionDeclarationBinder(decl),
        FunctionBodySyntax body => this.BuildFunctionBodyBinder(body),
        BlockExpressionSyntax block => this.BuildLocalBinder(block),
        _ when BinderFacts.IsLoopBody(syntax) => this.BuildLoopBodyBinder(syntax),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private Binder BuildCompilationUnitBinder()
    {
        // We need to wrap up the module with builtins
        var binder = new IntrinsicsBinder(this.compilation) as Binder;
        // Finally add the module
        binder = new ModuleBinder(binder, this.compilation.GlobalModule);

        return binder;
    }

    private Binder BuildFunctionDeclarationBinder(FunctionDeclarationSyntax syntax)
    {
        Debug.Assert(syntax.Parent is not null);
        var parent = this.GetBinder(syntax.Parent);
        // Search for the function in the parents container
        var functionSymbol = parent.ContainingSymbol?.Members
            .OfType<SourceFunctionSymbol>()
            .FirstOrDefault(member => member.DeclarationSyntax == syntax);
        Debug.Assert(functionSymbol is not null);
        return new FunctionBinder(parent, functionSymbol);
    }

    private Binder BuildFunctionBodyBinder(FunctionBodySyntax syntax)
    {
        Debug.Assert(syntax.Parent is not null);
        var parent = this.GetBinder(syntax.Parent);
        return new LocalBinder(parent, syntax);
    }

    private Binder BuildLocalBinder(BlockExpressionSyntax syntax)
    {
        Debug.Assert(syntax.Parent is not null);
        var parent = this.GetBinder(syntax.Parent);
        var binder = new LocalBinder(parent, syntax) as Binder;
        // If this is a loop body, wrap it up
        if (BinderFacts.IsLoopBody(syntax)) binder = new LoopBodyBinder(binder);
        return binder;
    }

    private Binder BuildLoopBodyBinder(SyntaxNode syntax)
    {
        Debug.Assert(syntax.Parent is not null);
        var parent = this.GetBinder(syntax.Parent);
        return new LoopBodyBinder(parent);
    }
}
