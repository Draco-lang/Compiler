using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds on a function level, including its parameters.
/// </summary>
internal sealed class FunctionBinder(Binder parent, FunctionSymbol symbol) : Binder(parent)
{
    public override Symbol? ContainingSymbol => this.symbol;

    public override SyntaxNode? DeclaringSyntax => this.symbol.DeclaringSyntax;

    public override IEnumerable<Symbol> DeclaredSymbols => this.symbol.Parameters
        .Cast<Symbol>()
        .Concat(this.symbol.GenericParameters);

    private readonly FunctionSymbol symbol = symbol;

    internal override void LookupLocal(LookupResult result, string name, ref LookupFlags flags, Predicate<Symbol> allowSymbol, SyntaxNode? currentReference)
    {
        // Check type parameters
        foreach (var typeParam in this.symbol.GenericParameters)
        {
            if (typeParam.Name != name) continue;
            if (!allowSymbol(typeParam)) continue;
            result.Add(typeParam);
            break;
        }

        if (flags.HasFlag(LookupFlags.DisallowLocals)) return;

        // Check parameters
        // We go in reverse-order, as these are technically locals
        // In case there are duplicate parameters, we resolve to the last one only,
        // To be closer to the shadowing semantics we have for locals
        foreach (var param in this.symbol.Parameters.Reverse())
        {
            if (param.Name != name) continue;
            if (!allowSymbol(param)) continue;
            result.Add(param);
            break;
        }

        // From now on we disallow locals
        // We also allow globals to be referenced, globals can only be referenced from function-local context
        flags |= LookupFlags.DisallowLocals | LookupFlags.AllowGlobals;
    }
}
