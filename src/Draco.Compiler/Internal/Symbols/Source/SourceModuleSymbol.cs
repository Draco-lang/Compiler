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
    public override ImmutableArray<FunctionSymbol> Functions => this.functions ??= this.BuildFunctions();
    private ImmutableArray<FunctionSymbol>? functions;

    public override ImmutableArray<GlobalSymbol> Globals => this.globals ??= this.BuildGlobals();
    private ImmutableArray<GlobalSymbol>? globals;

    public override IEnumerable<Symbol> Members => this.Functions
        .Cast<Symbol>()
        .Concat(this.Globals);

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

    private ImmutableArray<FunctionSymbol> BuildFunctions() => this.declaration.Children
        .OfType<FunctionDeclaration>()
        .Select(this.BuildFunction)
        .ToImmutableArray();

    private ImmutableArray<GlobalSymbol> BuildGlobals() => this.declaration.Children
        .OfType<GlobalDeclaration>()
        .Select(this.BuildGlobal)
        .ToImmutableArray();

    private FunctionSymbol BuildFunction(FunctionDeclaration declaration) => new SourceFunctionSymbol(this, declaration);
    private GlobalSymbol BuildGlobal(GlobalDeclaration declaration) => new SourceGlobalSymbol(this, declaration);
}
