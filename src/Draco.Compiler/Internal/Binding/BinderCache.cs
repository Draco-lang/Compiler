using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols.Script;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Syntax;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Responsible for caching the binders for syntax nodes and declarations.
/// </summary>
internal sealed class BinderCache(Compilation compilation)
{
    private readonly Compilation compilation = compilation;
    private readonly ConcurrentDictionary<SyntaxNode, Binder> binders = new();

    /// <summary>
    /// Retrieves a <see cref="Binder"/> for the given syntax node.
    /// </summary>
    /// <param name="syntax">The syntax node to retrieve the binder for.</param>
    /// <returns>The binder for <paramref name="syntax"/>.</returns>
    public Binder GetBinder(SyntaxNode syntax)
    {
        if(syntax is ClassDeclarationSyntax )
        {

        }
        var scopeDefiningAncestor = BinderFacts.GetScopeDefiningAncestor(syntax);
        Debug.Assert(scopeDefiningAncestor is not null);

        return this.binders.GetOrAdd(scopeDefiningAncestor, this.BuildBinder);
    }

    private Binder BuildBinder(SyntaxNode syntax) => syntax switch
    {
        CompilationUnitSyntax cu => this.BuildCompilationUnitBinder(cu),
        ScriptEntrySyntax entry => this.BuildScriptEntryBinder(entry),
        ModuleDeclarationSyntax mo => this.BuildModuleBinder(mo),
        FunctionDeclarationSyntax funcDecl => this.BuildFunctionDeclarationBinder(funcDecl),
        ClassDeclarationSyntax classDecl => this.BuildClassDeclarationBinder(classDecl),
        FunctionBodySyntax body => this.BuildFunctionBodyBinder(body),
        BlockExpressionSyntax block => this.BuildLocalBinder(block),
        WhileExpressionSyntax loop => this.BuildLoopBinder(loop),
        ForExpressionSyntax loop => this.BuildLoopBinder(loop),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private Binder BuildCompilationUnitBinder(CompilationUnitSyntax syntax)
    {
        var binder = new IntrinsicsBinder(this.compilation) as Binder;
        if (!this.compilation.GlobalImports.IsDefault) binder = new GlobalImportsBinder(binder);
        binder = new ModuleBinder(binder, this.compilation.RootModule);
        binder = new ModuleBinder(binder, this.compilation.GetModuleForSyntaxTree(syntax.Tree));
        binder = WrapInImportBinder(binder, syntax);
        return binder;
    }

    private Binder BuildScriptEntryBinder(ScriptEntrySyntax syntax)
    {
        var binder = new IntrinsicsBinder(this.compilation) as Binder;
        if (!this.compilation.GlobalImports.IsDefault) binder = new GlobalImportsBinder(binder);
        binder = new ModuleBinder(binder, this.compilation.RootModule);
        binder = new ScriptModuleBinder(binder, (ScriptModuleSymbol)this.compilation.SourceModule);
        binder = WrapInImportBinder(binder, syntax);
        return binder;
    }

    private ModuleBinder BuildModuleBinder(ModuleDeclarationSyntax syntax)
    {
        Debug.Assert(syntax.Parent is not null);
        var binder = this.GetBinder(syntax.Parent);
        // Search for the module in the parents container
        // For that we unwrap from the injected import layer(s)
        var parent = UnwrapFromImportBinder(binder);
        var moduleSymbol = parent.DeclaredSymbols
            .OfType<SourceModuleSymbol>()
            .FirstOrDefault(member => member.Name == syntax.Name.Text);
        Debug.Assert(moduleSymbol is not null);
        binder = WrapInImportBinder(binder, syntax);
        return new ModuleBinder(binder, moduleSymbol);
    }

    private FunctionBinder BuildFunctionDeclarationBinder(FunctionDeclarationSyntax syntax)
    {
        Debug.Assert(syntax.Parent is not null);
        var binder = this.GetBinder(syntax.Parent);
        // Search for the function in the parents container
        // For that we unwrap from the injected import layer(s)
        var parent = UnwrapFromImportBinder(binder);
        var functionSymbol = parent.DeclaredSymbols
            .OfType<SyntaxFunctionSymbol>()
            .FirstOrDefault(member => member.DeclaringSyntax == syntax);
        Debug.Assert(functionSymbol is not null);
        // NOTE: We are not using the unwrapped parent, we need the injected import layers
        return new FunctionBinder(binder, functionSymbol);
    }

    private ClassBinder BuildClassDeclarationBinder(ClassDeclarationSyntax syntax)
    {
        Debug.Assert(syntax.Parent is not null);
        var binder = this.GetBinder(syntax.Parent);

        var parent = UnwrapFromImportBinder(binder);
        var classSymbol = parent.DeclaredSymbols
            .OfType<SourceClassSymbol>()
            .FirstOrDefault(member => member.DeclaringSyntax == syntax); // should we shove that in an helper ?
        return new ClassBinder(binder, classSymbol);
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

    private LoopBinder BuildLoopBinder(SyntaxNode syntax)
    {
        Debug.Assert(syntax.Parent is not null);
        var parent = this.GetBinder(syntax.Parent);
        return syntax switch
        {
            ForExpressionSyntax @for => new ForLoopBinder(parent, @for),
            _ => new LoopBinder(parent, syntax),
        };
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
    private static Binder UnwrapFromImportBinder(Binder binder) => binder.AncestorChain
        .SkipWhile(b => b is ImportBinder)
        .First();
}
