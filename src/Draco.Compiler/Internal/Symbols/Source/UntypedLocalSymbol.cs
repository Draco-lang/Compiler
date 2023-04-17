using System;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// Represents an in-source local that doesn't have its type inferred yet.
/// </summary>
internal sealed class UntypedLocalSymbol : Symbol, ISourceSymbol
{
    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DeclarationSyntax.Name.Text;

    public override VariableDeclarationSyntax DeclarationSyntax { get; }

    public bool IsMutable => this.DeclarationSyntax.Keyword.Kind == TokenKind.KeywordVar;

    public UntypedLocalSymbol(Symbol? containingSymbol, VariableDeclarationSyntax syntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclarationSyntax = syntax;
    }

    public override ISymbol ToApiSymbol() => throw new NotSupportedException();
    public void Bind(DiagnosticBag diagnostics) => throw new NotSupportedException();
    public override void Accept(SymbolVisitor visitor) => throw new NotSupportedException();
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new NotSupportedException();
}
