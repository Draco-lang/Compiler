using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Declarations;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A module defined in-source.
/// </summary>
internal sealed class SourceModuleSymbol : ModuleSymbol
{
    public override Compilation DeclaringCompilation { get; }

    public override IEnumerable<Symbol> Members => this.members ??= this.BuildMembers();
    private ImmutableArray<Symbol>? members;

    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.declaration.Name;

    public override SyntaxNode? DeclarationSyntax => null;

    private readonly Declaration declaration;

    private SourceModuleSymbol(
        Compilation compilation,
        Symbol? containingSymbol,
        Declaration declaration)
    {
        this.DeclaringCompilation = compilation;
        this.ContainingSymbol = containingSymbol;
        this.declaration = declaration;
    }

    public SourceModuleSymbol(
        Compilation compilation,
        Symbol? containingSymbol,
        SingleModuleDeclaration declaration)
        : this(compilation, containingSymbol, declaration as Declaration)
    {
    }

    public SourceModuleSymbol(
        Compilation compilation,
        Symbol? containingSymbol,
        MergedModuleDeclaration declaration)
        : this(compilation, containingSymbol, declaration as Declaration)
    {
    }

    private ImmutableArray<Symbol> BuildMembers()
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();
        var diagnostics = this.DeclaringCompilation.GlobalDiagnosticBag;

        // Syntax-declaration
        foreach (var declaration in this.declaration.Children)
        {
            var member = this.BuildMember(declaration);
            var earlierMember = result.FirstOrDefault(s => s.Name == member.Name);
            result.Add(member);

            // We chech for illegal shadowing
            if (earlierMember is null) continue;

            // Overloading is legal
            if (member is FunctionSymbol && earlierMember is FunctionSymbol) continue;

            // Illegal
            var syntax = member.DeclarationSyntax;
            Debug.Assert(syntax is not null);
            diagnostics.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.IllegalShadowing,
                location: syntax.Location,
                formatArgs: member.Name));
        }

        return result.ToImmutable();
    }

    private Symbol BuildMember(Declaration declaration) => declaration switch
    {
        FunctionDeclaration f => this.BuildFunction(f),
        GlobalDeclaration g => this.BuildGlobal(g),
        _ => throw new ArgumentOutOfRangeException(nameof(declaration)),
    };

    private FunctionSymbol BuildFunction(FunctionDeclaration declaration) => new SourceFunctionSymbol(this, declaration);
    private GlobalSymbol BuildGlobal(GlobalDeclaration declaration) => new SourceGlobalSymbol(this, declaration);
}
