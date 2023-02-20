using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds on a function level, including its parameters.
/// </summary>
internal sealed class FunctionBinder : Binder
{
    private readonly FunctionSymbol symbol;

    public FunctionBinder(Binder parent, FunctionSymbol symbol)
        : base(parent)
    {
        this.symbol = symbol;
    }

    protected override void LookupValueSymbol(LookupResult result, string name, SyntaxNode? reference) =>
        throw new NotImplementedException();

    protected override void LookupTypeSymbol(LookupResult result, string name, SyntaxNode? reference) =>
        throw new NotImplementedException();
}
