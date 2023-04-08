using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

internal sealed class SynthetizedModuleSymbol : ModuleSymbol
{
    public override Symbol? ContainingSymbol { get; }
    public override string Name { get; }
    public override IEnumerable<Symbol> Members { get; }

    public SynthetizedModuleSymbol(Symbol? containingSymbol, string name, ImmutableArray<Symbol> members)
    {
        this.ContainingSymbol = containingSymbol;
        this.Name = name;
        this.Members = members;
    }

    public override ISymbol ToApiSymbol() => throw new NotImplementedException();
}
