using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds on a module level.
/// </summary>
internal sealed class ModuleBinder : Binder
{
    private readonly ModuleSymbol symbol;

    public ModuleBinder(Binder parent, ModuleSymbol symbol)
        : base(parent)
    {
        this.symbol = symbol;
    }

    protected override void LookupSymbolsLocally(LookupResult result, string name, SymbolFilter filter, SyntaxNode? reference) =>
        LookupSymbolsLocallyTrivial(this.symbol.Members, result, name, filter, reference);
}
