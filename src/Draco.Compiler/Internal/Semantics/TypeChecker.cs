using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Query;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// Implements type-checking.
/// </summary>
internal static class TypeChecker
{
    /// <summary>
    /// Evaluates the given type expression node to the compiler representation of a type.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="expr">The <see cref="ParseTree.TypeExpr"/> to evaluate.</param>
    /// <returns>The <see cref="Type"/> representation of <paramref name="expr"/>.</returns>
    public static Type Evaluate(QueryDatabase db, ParseTree.TypeExpr expr) => db.GetOrUpdate(
        expr,
        Type (expr) => expr switch
        {
            // TODO: This is a temporary solution
            // Later, we'll need symbol resolution to be able to reference type-symbols only and such
            // For now this is a simple, greedy workaround
            ParseTree.TypeExpr.Name namedType => SymbolResolution.GetReferencedSymbol(db, namedType) is Symbol.TypeAlias typeAlias
                ? typeAlias.Type
                : throw new InvalidOperationException(),
            _ => throw new ArgumentOutOfRangeException(nameof(expr)),
        });
}
