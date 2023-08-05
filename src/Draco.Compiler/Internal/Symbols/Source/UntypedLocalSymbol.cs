using System;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// Represents an in-source local that doesn't have its type inferred yet.
/// </summary>
internal sealed class UntypedLocalSymbol : Symbol, ISourceSymbol
{
    public override Symbol ContainingSymbol { get; }
    public override string Name { get; }
    public bool IsMutable { get; }

    public override SyntaxNode DeclaringSyntax { get; }

    public UntypedLocalSymbol(Symbol containingSymbol, string name, bool isMutable, SyntaxNode declaringSyntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.Name = name;
        this.IsMutable = isMutable;
        this.DeclaringSyntax = declaringSyntax;
    }

    public UntypedLocalSymbol(Symbol containingSymbol, VariableDeclarationSyntax syntax)
        : this(containingSymbol, syntax.Name.Text, syntax.Keyword.Kind == TokenKind.KeywordVar, syntax)
    {
    }

    public override ISymbol ToApiSymbol() => throw new NotSupportedException();
    public void Bind(IBinderProvider binderProvider) => throw new NotSupportedException();
    public override void Accept(SymbolVisitor visitor) => throw new NotSupportedException();
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new NotSupportedException();
}
