using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds on a module level.
/// </summary>
internal sealed class ModuleBinder : Binder
{
    public override Symbol? ContainingSymbol => this.symbol;

    private readonly ModuleSymbol symbol;

    public ModuleBinder(Compilation compilation, ModuleSymbol symbol)
        : base(compilation)
    {
        this.symbol = symbol;
    }

    public ModuleBinder(Binder parent, ModuleSymbol symbol)
        : base(parent)
    {
        this.symbol = symbol;
    }

    public override void LookupValueSymbol(LookupResult result, string name, SyntaxNode? reference)
    {
        foreach (var symbol in this.symbol.Members)
        {
            if (symbol.Name != name) continue;
            if (!BinderFacts.IsValueSymbol(symbol)) continue;
            result.Add(symbol);
        }

        // TODO: Look at TODO in FunctionBinder

        // If we are collecting an overload-set or the result is empty, we try to continue upwards
        // Otherwise we can stop
        if (!result.FoundAny || result.IsOverloadSet)
        {
            var parentReference = BinderFacts.GetScopeDefiningAncestor(reference?.Parent);
            this.Parent?.LookupValueSymbol(result, name, parentReference);
        }
    }

    public override void LookupTypeSymbol(LookupResult result, string name, SyntaxNode? reference) =>
        throw new NotImplementedException();
}
