using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    public Binder ModuleBinder => this.moduleBinder ??= this.BuildSourceModuleBinder();
    private Binder? moduleBinder;

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
        CompilationUnitSyntax cu => this.BuildCompilationUnitBinder(cu),
        FunctionDeclarationSyntax decl => this.BuildFunctionDeclarationBinder(decl),
        FunctionBodySyntax body => this.BuildFunctionBodyBinder(body),
        BlockExpressionSyntax block => this.BuildLocalBinder(block),
        WhileExpressionSyntax loop => this.BuildLoopBinder(loop),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private Binder BuildSourceModuleBinder()
    {
        // We need to wrap up the module with builtins
        var binder = new IntrinsicsBinder(this.compilation) as Binder;
        // Then with references
        binder = new ModuleBinder(binder, this.compilation.RootModule);
        // Finally add the source module
        binder = new ModuleBinder(binder, this.compilation.SourceModule);

        return binder;
    }

    private Binder BuildCompilationUnitBinder(CompilationUnitSyntax syntax)
    {
        var binder = new IntrinsicsBinder(this.compilation) as Binder;
        binder = new ModuleBinder(binder, this.compilation.RootModule);
        binder = new ModuleBinder(binder, this.compilation.GetCompilationUnitModule(syntax.Tree));
        binder = WrapInImportBinder(binder, syntax);
        return binder;
    }

    private Binder BuildFunctionDeclarationBinder(FunctionDeclarationSyntax syntax)
    {
        Debug.Assert(syntax.Parent is not null);
        var binder = this.GetBinder(syntax.Parent);
        // Search for the function in the parents container
        // For that we unwrap from the injected import layer(s)
        var parent = UnwrapFromImportBinder(binder);
        var functionSymbol = parent.DeclaredSymbols
            .OfType<SourceFunctionSymbol>()
            .FirstOrDefault(member => member.DeclaringSyntax == syntax);
        Debug.Assert(functionSymbol is not null);
        // NOTE: We are not using the unwrapped parent, we need the injected import layers
        return new FunctionBinder(binder, functionSymbol);
    }

    private Binder BuildFunctionBodyBinder(FunctionBodySyntax syntax)
    {
        Debug.Assert(syntax.Parent is not null);
        var binder = this.GetBinder(syntax.Parent);
        binder = WrapInImportBinder(binder, syntax);
        binder = new LocalBinder(binder, syntax);
        return binder;
    }

    private Binder BuildLocalBinder(BlockExpressionSyntax syntax)
    {
        Debug.Assert(syntax.Parent is not null);
        var binder = this.GetBinder(syntax.Parent);
        binder = WrapInImportBinder(binder, syntax);
        binder = new LocalBinder(binder, syntax);
        return binder;
    }

    private Binder BuildLoopBinder(SyntaxNode syntax)
    {
        Debug.Assert(syntax.Parent is not null);
        var parent = this.GetBinder(syntax.Parent);
        return new LoopBinder(parent, syntax);
    }

    /// <summary>
    /// Wraps the given binder into an import binder, if the given syntax contains imports.
    /// </summary>
    /// <param name="binder">The binder to wrap.</param>
    /// <param name="syntax">The syntax to check for imports.</param>
    /// <returns>The <paramref name="binder"/> wrapped up in an import binder, if needed, otherwise
    /// the <paramref name="binder"/> itself.</returns>
    private static Binder WrapInImportBinder(Binder binder, SyntaxNode syntax)
    {
        var hasImportSyntaxes = BinderFacts.EnumerateNodesInSameScope(syntax)
            .OfType<ImportDeclarationSyntax>()
            .Any();
        return hasImportSyntaxes
            ? new ImportBinder(binder, syntax)
            : binder;
    }

    /// <summary>
    /// Unwraps a binder from import nesting.
    /// </summary>
    /// <param name="binder">The binder to unwrap.</param>
    /// <returns>The binder that was wrapped in imports.</returns>
    private static Binder UnwrapFromImportBinder(Binder binder)
    {
        while (binder is ImportBinder)
        {
            binder = binder.Parent ?? throw new InvalidOperationException();
        }
        return binder;
    }
}
