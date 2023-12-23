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
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;
using Draco.Compiler.Internal.Symbols.Synthetized;

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

    public override SymbolDocumentation Documentation => InterlockedUtils.InitializeNull(ref this.documentation, this.BuildDocumentation);
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
            var members = this.BuildMember(declaration);
            foreach (var member in members)
            {
                var earlierMember = result.FirstOrDefault(s => s.Name == member.Name);
                result.Add(member);

                // Overloading is legal, shadowing is checked by the functions themselves
                if (member is FunctionSymbol && earlierMember is FunctionSymbol) continue;

                // We chech for illegal shadowing
                if (earlierMember is null) continue;
                if (!earlierMember.CanBeShadowedBy(member)) continue;

                // Illegal
                var syntax = member.DeclaringSyntax;
                Debug.Assert(syntax is not null);
                binderProvider.DiagnosticBag.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.IllegalShadowing,
                    location: syntax.Location,
                    formatArgs: member.Name));
            }
        }

        return result.ToImmutable();
    }

    private IEnumerable<Symbol> BuildMember(Declaration declaration) => declaration switch
    {
        FunctionDeclaration f => new[] { this.BuildFunction(f) },
        GlobalDeclaration g => new[] { this.BuildGlobal(g) },
        MergedModuleDeclaration m => new[] { this.BuildModule(m) },
        ClassDeclaration c => this.BuildClass(c),
        _ => throw new ArgumentOutOfRangeException(nameof(declaration)),
    };

    private FunctionSymbol BuildFunction(FunctionDeclaration declaration) => new SourceFunctionSymbol(this, declaration);
    private GlobalSymbol BuildGlobal(GlobalDeclaration declaration) => new SourceGlobalSymbol(this, declaration);
    private ModuleSymbol BuildModule(MergedModuleDeclaration declaration) => new SourceModuleSymbol(this.DeclaringCompilation, this, declaration);
    private IEnumerable<Symbol> BuildClass(ClassDeclaration declaration)
    {
        var result = new List<Symbol>();
        var classSymbol = new SourceClassSymbol(this, declaration);
        result.Add(classSymbol);
        // Add constructor functions
        foreach (var ctor in classSymbol.Constructors)
        {
            var ctorSymbol = new ConstructorFunctionSymbol(ctor);
            result.Add(ctorSymbol);
        }
        return result;
    }

    private SymbolDocumentation BuildDocumentation() =>
        MarkdownDocumentationExtractor.Extract(this);
}
