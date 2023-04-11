using System;
using System.Collections.Generic;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds the break and continue labels in the loop body.
/// </summary>
internal sealed class LoopBinder : Binder
{
    /// <summary>
    /// The break label.
    /// </summary>
    public LabelSymbol BreakLabel { get; } = new SynthetizedLabelSymbol("break");

    /// <summary>
    /// The continue label.
    /// </summary>
    public LabelSymbol ContinueLabel { get; } = new SynthetizedLabelSymbol("continue");

    public override SyntaxNode DeclaringSyntax { get; }

    public override IEnumerable<Symbol> DeclaredSymbols => new[] { this.BreakLabel, this.ContinueLabel };

    public LoopBinder(Binder parent, SyntaxNode declaringSyntax)
        : base(parent)
    {
        this.DeclaringSyntax = declaringSyntax;
    }

    internal override void LookupLocal(LookupResult result, string name, ref LookupFlags flags, Predicate<Symbol> allowSymbol, SyntaxNode? currentReference)
    {
        if (flags.HasFlag(LookupFlags.DisallowLocals)) return;

        if (name == this.BreakLabel.Name && allowSymbol(this.BreakLabel)) result.Add(this.BreakLabel);
        if (name == this.ContinueLabel.Name && allowSymbol(this.ContinueLabel)) result.Add(this.ContinueLabel);
    }
}
