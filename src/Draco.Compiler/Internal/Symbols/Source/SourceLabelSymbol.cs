using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source label definition.
/// </summary>
internal sealed class SourceLabelSymbol : LabelSymbol, ISourceSymbol
{
    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DeclarationSyntax.Name.Text;

    public LabelDeclarationSyntax DeclarationSyntax { get; }
    SyntaxNode ISourceSymbol.DeclarationSyntax => this.DeclarationSyntax;

    public SourceLabelSymbol(Symbol? containingSymbol, LabelDeclarationSyntax declarationSyntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclarationSyntax = declarationSyntax;
    }

    public override ISymbol ToApiSymbol() => new Api.Semantics.LabelSymbol(this);
}
