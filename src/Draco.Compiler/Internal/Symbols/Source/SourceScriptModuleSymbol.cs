using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Declarations;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A module defined by a script.
/// </summary>
internal sealed class SourceScriptModuleSymbol : ModuleSymbol, ISourceSymbol
{
    public override Compilation DeclaringCompilation { get; }

    public override IEnumerable<Symbol> Members => this.BindMembersIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<Symbol> members;

    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DeclaringCompilation.RootModulePath;

    private readonly ScriptEntrySyntax syntax;

    public SourceScriptModuleSymbol(
        Compilation compilation,
        Symbol? containingSymbol,
        ScriptEntrySyntax syntax)
    {
        this.DeclaringCompilation = compilation;
        this.ContainingSymbol = containingSymbol;
        this.syntax = syntax;
    }

    public void Bind(IBinderProvider binderProvider) =>
        this.BindMembersIfNeeded(binderProvider);

    private ImmutableArray<Symbol> BindMembersIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeDefault(ref this.members, () => this.BindMembers(binderProvider));

    private ImmutableArray<Symbol> BindMembers(IBinderProvider binderProvider)
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();

        // Statements
        foreach (var statement in this.syntax.Statements)
        {
            // Non-declaration statements are compiled into an initializer method
            if (statement is not DeclarationStatementSyntax decl) continue;

            // Build the declaration
            var symbol = this.BuildMember(decl.Declaration);
            result.Add(symbol);
        }

        // If there is a value, we need to synthetize a function to evaluate it
        if (this.syntax.Value is not null)
        {
            // TODO
            throw new NotImplementedException();
        }

        return result.ToImmutable();
    }

    private Symbol BuildMember(DeclarationSyntax decl) => decl switch
    {
        FunctionDeclarationSyntax f => this.BuildFunction(f),
        VariableDeclarationSyntax v => this.BuildGlobal(v),
        ModuleDeclarationSyntax m => this.BuildModule(m),
        _ => throw new ArgumentOutOfRangeException(nameof(decl)),
    };

    private SourceFunctionSymbol BuildFunction(FunctionDeclarationSyntax syntax) => new(this, syntax);
    private SourceGlobalSymbol BuildGlobal(VariableDeclarationSyntax syntax) => new(this, syntax);

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
