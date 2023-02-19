using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Declarations;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A module defined in-source.
/// </summary>
internal sealed class SourceModuleSymbol : ModuleSymbol
{
    public override ImmutableArray<Symbol> Members => this.members ??= this.BuildMembers();
    private ImmutableArray<Symbol>? members;

    public override Symbol? ContainingSymbol { get; }
    public override string Name { get; }

    private readonly Declaration declaration;

    private SourceModuleSymbol(Symbol? containingSymbol, string name, Declaration declaration)
    {
        this.ContainingSymbol = containingSymbol;
        this.Name = name;
        this.declaration = declaration;
    }

    public SourceModuleSymbol(Symbol? containingSymbol, SingleModuleDeclaration declaration)
        : this(containingSymbol, declaration.Name, declaration)
    {
    }

    public SourceModuleSymbol(Symbol? containingSymbol, MergedModuleDeclaration declaration)
        : this(containingSymbol, declaration.Name, declaration)
    {
    }

    private ImmutableArray<Symbol> BuildMembers() => this.declaration.Children
        .Select(this.BuildMember)
        .ToImmutableArray();

    private Symbol BuildMember(Declaration declaration) => declaration switch
    {
        FunctionDeclaration f => this.BuildFunction(f),
        GlobalDeclaration g => this.BuildGlobal(g),
        _ => throw new ArgumentOutOfRangeException(nameof(declaration)),
    };

    private FunctionSymbol BuildFunction(FunctionDeclaration declaration) => new SourceFunctionSymbol(this, declaration);
    private GlobalSymbol BuildGlobal(GlobalDeclaration declaration) => new SourceGlobalSymbol(this, declaration);
}
