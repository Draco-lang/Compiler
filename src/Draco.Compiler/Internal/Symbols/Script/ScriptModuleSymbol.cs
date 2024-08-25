using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Symbols.Script;

/// <summary>
/// A module defined by a script.
/// </summary>
internal sealed class ScriptModuleSymbol(
    Compilation compilation,
    Symbol? containingSymbol,
    ScriptEntrySyntax syntax) : ModuleSymbol, ISourceSymbol
{
    public override Compilation DeclaringCompilation => compilation;
    public override ScriptEntrySyntax DeclaringSyntax => syntax;

    public override IEnumerable<Symbol> Members => this.BindMembersIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<Symbol> members;

    public override Symbol? ContainingSymbol { get; } = containingSymbol;
    public override string Name => this.DeclaringCompilation.RootModulePath;

    /// <summary>
    /// The evaluation function of the script.
    /// </summary>
    public ScriptEvalFunctionSymbol EvalFunction => this.Members.OfType<ScriptEvalFunctionSymbol>().Single();

    /// <summary>
    /// The imports of the script.
    /// </summary>
    public IEnumerable<ImportDeclarationSyntax> Imports => this.DeclaringSyntax.Statements
        .OfType<DeclarationStatementSyntax>()
        .Select(s => s.Declaration)
        .OfType<ImportDeclarationSyntax>();

    public void Bind(IBinderProvider binderProvider) =>
        this.BindMembersIfNeeded(binderProvider);

    private ImmutableArray<Symbol> BindMembersIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeDefault(ref this.members, () => this.BindMembers(binderProvider));

    private ImmutableArray<Symbol> BindMembers(IBinderProvider binderProvider)
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();

        // Statements
        foreach (var statement in this.DeclaringSyntax.Statements)
        {
            // Non-declaration statements are compiled into an initializer method
            if (statement is not DeclarationStatementSyntax decl) continue;

            // Build the declaration
            var member = this.BuildMember(decl.Declaration);
            var earlierMember = result.FirstOrDefault(s => s.Name == member.Name);
            result.Add(member);

            // We check for illegal shadowing
            if (earlierMember is null) continue;

            // Overloading is legal
            if (member is FunctionSymbol && earlierMember is FunctionSymbol) continue;

            // Illegal
            var syntax = member.DeclaringSyntax;
            Debug.Assert(syntax is not null);
            binderProvider.DiagnosticBag.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.IllegalShadowing,
                location: syntax.Location,
                formatArgs: member.Name));
        }

        // We add a function to evaluate the script
        var evalFunction = new ScriptEvalFunctionSymbol(this, this.DeclaringSyntax);
        result.Add(evalFunction);

        return result.ToImmutable();
    }

    private Symbol BuildMember(DeclarationSyntax decl) => decl switch
    {
        FunctionDeclarationSyntax f => this.BuildFunction(f),
        VariableDeclarationSyntax v => this.BuildGlobal(v),
        ModuleDeclarationSyntax m => this.BuildModule(m),
        _ => throw new ArgumentOutOfRangeException(nameof(decl)),
    };

    private ScriptFunctionSymbol BuildFunction(FunctionDeclarationSyntax syntax) => new(this, syntax);
    private ScriptGlobalSymbol BuildGlobal(VariableDeclarationSyntax syntax) => new(this, syntax);

    private SourceModuleSymbol BuildModule(ModuleDeclarationSyntax syntax)
    {
        // We need to wrap it into a merged module declaration
        var name = syntax.Name.Text;
        var path = SplitPath.FromParts(this.DeclaringCompilation.RootModulePath, syntax.Name.Text);
        var declaration = new MergedModuleDeclaration(
            name: name,
            path: path,
            declarations: [
                new SingleModuleDeclaration(
                    name: name,
                    path,
                    syntax)]);
        return new SourceModuleSymbol(this.DeclaringCompilation, this, declaration);
    }
}
