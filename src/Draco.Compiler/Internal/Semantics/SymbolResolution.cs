using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Query;
using Draco.Query.Tasks;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// Implements lazy symbol resolution.
/// </summary>
internal static class SymbolResolution
{
    public static async QueryValueTask<Scope> GetContainingScope(QueryDatabase db, ParseTree tree)
    {
        throw new NotImplementedException();
    }

    public static async QueryValueTask<Scope?> GetDefinedScope(QueryDatabase db, ParseTree tree)
    {
        throw new NotImplementedException();
    }

    public static async QueryValueTask<Symbol?> GetDefinedSymbol(QueryDatabase db, ParseTree tree)
    {
        throw new NotImplementedException();
    }

    public static async QueryValueTask<Symbol?> ReferenceSymbol(QueryDatabase db, ParseTree tree, string name)
    {
        throw new NotImplementedException();
    }
}
