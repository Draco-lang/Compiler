using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a compilation unit.
/// </summary>
internal abstract partial class ModuleSymbol : Symbol, IMemberSymbol
{
    public override Visibility Visibility => this.Members.Any(x => x.Visibility == Visibility.Public) ? Visibility.Public : Visibility.Internal;

    public bool IsStatic => true;

    public override void Accept(SymbolVisitor visitor) => visitor.VisitModule(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitModule(this);

    public override ISymbol ToApiSymbol() => new Api.Semantics.ModuleSymbol(this);

    // TODO: Doc, make up something nicer?
    // NOTE: Some very janky lookup capability
    public IEnumerable<Symbol> Lookup(ImmutableArray<string> parts)
    {
        if (parts.Length == 0) return new[] { this };

        var current = this as Symbol;
        for (var i = 0; i < parts.Length - 1; ++i)
        {
            var part = parts[i];
            current = current.Members
                .Where(m => m.MetadataName == part && m is ModuleSymbol or TypeSymbol)
                .Single();
        }

        return current.Members.Where(m => m.MetadataName == parts[^1]);
    }
}
