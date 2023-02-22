using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

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

    public override void LookupValueSymbol(LookupResult result, string name, SyntaxNode? reference) =>
        throw new NotImplementedException();

    public override void LookupTypeSymbol(LookupResult result, string name, SyntaxNode? reference) =>
        throw new NotImplementedException();
}
