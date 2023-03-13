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

    public override IEnumerable<Symbol> Symbols => this.symbol.Parameters;

    private readonly FunctionSymbol symbol;

    public FunctionBinder(Binder parent, FunctionSymbol symbol)
        : base(parent)
    {
        this.symbol = symbol;
    }

    public override void LookupValueSymbol(LookupResult result, string name, SyntaxNode? reference)
    {
        foreach (var param in this.symbol.Parameters)
        {
            if (param.Name != name) continue;
            result.Add(param);
        }

        // TODO: This is identical to the condition in the LocalBinder
        // While this might be the same in the end... From here on out (outside a function)
        // we can only reference global variables
        // Maybe the current lookup system is just bad and we should do it iteratively in the base class, carrying flags
        // and only ask each scope for their symbols

        // If we are collecting an overload-set or the result is empty, we try to continue upwards
        // Otherwise we can stop
        if (!result.FoundAny || result.IsOverloadSet)
        {
            var parentReference = BinderFacts.GetScopeDefiningAncestor(reference?.Parent);
            this.Parent?.LookupValueSymbol(result, name, parentReference);
        }
    }

    public override void LookupTypeSymbol(LookupResult result, string name, SyntaxNode? reference) =>
        this.Parent?.LookupTypeSymbol(result, name, reference);
}
