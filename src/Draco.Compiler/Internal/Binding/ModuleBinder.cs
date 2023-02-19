using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

internal sealed class ModuleBinder : Binder
{
    protected override IEnumerable<Symbol> Symbols => this.symbol.Members;

    private readonly ModuleSymbol symbol;

    public ModuleBinder(Binder parent, ModuleSymbol symbol)
        : base(parent)
    {
        this.symbol = symbol;
    }
}
