using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds compiler-intrinsic symbols
/// </summary>
internal sealed class IntrinsicsBinder : Binder
{
    private static ImmutableArray<Symbol> IntrinsicSymbols { get; } = ImmutableArray.Create(
        Intrinsics.Int32_Equal,
        Intrinsics.Int32_NotEqual,
        Intrinsics.Int32_GreaterThan,
        Intrinsics.Int32_LessThan,
        Intrinsics.Int32_GreaterEqual,
        Intrinsics.Int32_LessEqual);

    public IntrinsicsBinder(Compilation compilation)
        : base(compilation)
    {
    }

    public IntrinsicsBinder(Binder parent)
        : base(parent)
    {
    }

    public override void LookupValueSymbol(LookupResult result, string name, SyntaxNode? reference)
    {
        foreach (var symbol in IntrinsicSymbols)
        {
            if (symbol.Name != name) continue;
            if (!BinderFacts.IsValueSymbol(symbol)) continue;
            result.Add(symbol);
        }

        // TODO: Look at TODO in FunctionBinder  or ModuleBinder

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
