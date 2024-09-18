using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Binding;
internal sealed class ClassBinder(Binder parent, TypeSymbol symbol) : Binder(parent)
{
    public override Symbol? ContainingSymbol => this.symbol;
    public override SyntaxNode? DeclaringSyntax => this.symbol.DeclaringSyntax;

    private readonly Symbol symbol = symbol;

    public override IEnumerable<Symbol> DeclaredSymbols => this.symbol.Members
        .Cast<Symbol>()
        .Concat(this.symbol.GenericParameters);

    internal override void LookupLocal(LookupResult result, string name, ref LookupFlags flags, Predicate<Symbol> allowSymbol, SyntaxNode? currentReference)
    {
        // copied from the function binder, as far as i understand it, it should be the same.
        foreach (var typeParam in this.symbol.GenericParameters)
        {
            if (typeParam.Name != name) continue;
            if (!allowSymbol(typeParam)) continue;
            result.Add(typeParam);
            break;
        }

        if (flags.HasFlag(LookupFlags.DisallowLocals)) return;

        foreach (var member in this.symbol.Members)
        {
            if (member.Name != name) continue;
            if (!allowSymbol(member)) continue;
            result.Add(member);
            break;
        }

        flags |= LookupFlags.DisallowLocals | LookupFlags.AllowGlobals;
    }
}
