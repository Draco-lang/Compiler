using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds the iterator variable on top of <see cref="LoopBinder"/>.
/// </summary>
internal sealed class ForLoopBinder : LoopBinder
{
    /// <summary>
    /// The introduced iterator variable.
    /// </summary>
    public LocalSymbol Iterator { get; }

    public override ForExpressionSyntax DeclaringSyntax { get; }

    public override IEnumerable<Symbol> DeclaredSymbols =>
        base.DeclaredSymbols.Append(this.Iterator);

    public ForLoopBinder(Binder parent, ForExpressionSyntax declaringSyntax)
        : base(parent, declaringSyntax)
    {
        this.DeclaringSyntax = declaringSyntax;
        this.Iterator = new SourceLocalSymbol(
            this.ContainingSymbol!,
            declaringSyntax.Iterator.Text,
            type: new TypeVariable(0),
            isMutable: false,
            declaringSyntax.Iterator);
    }

    internal override void LookupLocal(LookupResult result, string name, ref LookupFlags flags, Predicate<Symbol> allowSymbol, SyntaxNode? currentReference)
    {
        if (flags.HasFlag(LookupFlags.DisallowLocals)) return;

        if (name == this.Iterator.Name && allowSymbol(this.Iterator)) result.Add(this.Iterator);
        base.LookupLocal(result, name, ref flags, allowSymbol, currentReference);
    }
}
