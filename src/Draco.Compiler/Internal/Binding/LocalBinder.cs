using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds local variables.
///
/// A local binder is a bit more complex because of shadowing. When a lookup happens, the relative position of
/// things have to be considered. Example:
///
/// var x = 0; // x1
/// {
///     var y = x; // y1 that references x1
/// }
/// var x = x; // x2 that references x1
/// var y = x; // y2 that references x2
/// </summary>
internal sealed class LocalBinder : Binder
{
    private readonly record struct LocalDeclaration(int Position, Symbol Symbol);

    private ImmutableArray<LocalDeclaration> LocalDeclarations => this.localDeclarations ??= this.BuildLocalDeclarations();
    private ImmutableArray<LocalDeclaration>? localDeclarations;

    private readonly BlockExpressionSyntax syntax;

    public LocalBinder(Binder parent, BlockExpressionSyntax syntax)
        : base(parent)
    {
        this.syntax = syntax;
    }

    protected override void LookupSymbolsLocally(LookupResult result, string name, SymbolFilter filter) =>
        throw new NotImplementedException();

    private ImmutableArray<LocalDeclaration> BuildLocalDeclarations() =>
        throw new NotImplementedException();
}
