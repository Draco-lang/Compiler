using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A module defined in-source.
/// </summary>
internal sealed class SourceModuleSymbol : ModuleSymbol, ISourceSymbol
{
    public override Compilation DeclaringCompilation { get; }

    public override IEnumerable<Symbol> Members => this.BindMembersIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<Symbol> members;

    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.declaration.Name;

    public override SymbolDocumentation Documentation => LazyInitializer.EnsureInitialized(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    /// <summary>
    /// The syntaxes contributing to this module.
    /// </summary>
    public IEnumerable<SyntaxNode> DeclaringSyntaxes => this.declaration.DeclaringSyntaxes;

    internal override string RawDocumentation => this.DeclaringSyntaxes
        .Select(syntax => syntax.Documentation)
        .Where(doc => !string.IsNullOrEmpty(doc))
        .FirstOrDefault() ?? string.Empty;

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
        MergedModuleDeclaration declaration)
        : this(compilation, containingSymbol, declaration as Declaration)
    {
    }

    public void Bind(IBinderProvider binderProvider) =>
        this.BindMembersIfNeeded(binderProvider);

    private ImmutableArray<Symbol> BindMembersIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeDefault(ref this.members, () => this.BindMembers(binderProvider));

    private ImmutableArray<Symbol> BindMembers(IBinderProvider binderProvider)
    {
        var result = ImmutableArray.CreateBuilder<Symbol>();

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
            var syntax = member.DeclaringSyntax;
            Debug.Assert(syntax is not null);
            binderProvider.DiagnosticBag.Add(Diagnostic.Create(
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
        MergedModuleDeclaration m => this.BuildModule(m),
        _ => throw new ArgumentOutOfRangeException(nameof(declaration)),
    };

    private FunctionSymbol BuildFunction(FunctionDeclaration declaration) => new SourceFunctionSymbol(this, declaration);
    private GlobalSymbol BuildGlobal(GlobalDeclaration declaration) => new SourceGlobalSymbol(this, declaration);
    private ModuleSymbol BuildModule(MergedModuleDeclaration declaration) => new SourceModuleSymbol(this.DeclaringCompilation, this, declaration);

    private SymbolDocumentation BuildDocumentation() =>
        MarkdownDocumentationExtractor.Extract(this);
}
