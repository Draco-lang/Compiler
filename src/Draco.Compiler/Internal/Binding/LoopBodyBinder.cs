using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds the break and continue labels in the loop body.
/// </summary>
internal sealed class LoopBodyBinder : Binder
{
    /// <summary>
    /// The break label.
    /// </summary>
    public LabelSymbol BreakLabel { get; } = new SynthetizedLabelSymbol("break");

    /// <summary>
    /// The continue label.
    /// </summary>
    public LabelSymbol ContinueLabel { get; } = new SynthetizedLabelSymbol("continue");

    public override IEnumerable<Symbol> DeclaredSymbols => new[] { this.BreakLabel, this.ContinueLabel };

    public LoopBodyBinder(Binder parent)
        : base(parent)
    {
    }

    public override void LookupValueSymbol(LookupResult result, string name, SyntaxNode? reference)
    {
        // TODO: Are labels values? Is this a good system we have currently?
        // Maybe we should just merge all lookup logic and start using flags...
        // With the exception of local binder, it shouldn't be that hard

        if (name == this.BreakLabel.Name) result.Add(this.BreakLabel);
        if (name == this.ContinueLabel.Name) result.Add(this.ContinueLabel);

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
