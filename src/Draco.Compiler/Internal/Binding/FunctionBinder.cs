using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds on a function level, including its parameters.
/// </summary>
internal sealed class FunctionBinder : Binder
{
    public override Symbol? ContainingSymbol => this.symbol;

    public override IEnumerable<Symbol> DeclaredSymbols => this.symbol.Parameters;

    private readonly FunctionSymbol symbol;

    public FunctionBinder(Binder parent, FunctionSymbol symbol)
        : base(parent)
    {
        this.symbol = symbol;
    }

    internal override void LookupLocal(LookupResult result, string name, ref LookupFlags flags, Predicate<Symbol> allowSymbol, SyntaxNode? currentReference)
    {
        if (flags.HasFlag(LookupFlags.DisallowLocals)) return;

        // Check parameters
        foreach (var param in this.symbol.Parameters)
        {
            if (param.Name != name) continue;
            if (!allowSymbol(param)) continue;
            result.Add(param);
        }

        // From now on we disallow locals
        flags |= LookupFlags.DisallowLocals;
    }
}
