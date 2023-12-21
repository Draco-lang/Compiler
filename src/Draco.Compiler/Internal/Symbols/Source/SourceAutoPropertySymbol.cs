using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An auto-prop coming from source.
/// </summary>
internal sealed class SourceAutoPropertySymbol : PropertySymbol, ISourceSymbol
{
    public override TypeSymbol ContainingSymbol { get; }

    public override TypeSymbol Type => throw new NotImplementedException();

    public override FunctionSymbol Getter => throw new NotImplementedException();
    public override FunctionSymbol? Setter => this.Modifiers.Keyword.Kind == TokenKind.KeywordVal
        ? null
        : throw new NotImplementedException();

    /// <summary>
    /// The backing field of this auto-prop.
    /// </summary>
    public FieldSymbol BackingField => throw new NotImplementedException();

    public override bool IsIndexer => false;
    public override bool IsStatic => false;

    // TODO: Not necessarily this type, only for primary constructors
    public override PrimaryConstructorParameterSyntax DeclaringSyntax { get; }
    private PrimaryConstructorParameterMemberModifiersSyntax Modifiers => this.DeclaringSyntax.MemberModifiers!;

    public SourceAutoPropertySymbol(TypeSymbol containingSymbol, PrimaryConstructorParameterSyntax declaringSyntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclaringSyntax = declaringSyntax;
    }

    public void Bind(IBinderProvider binderProvider) => throw new NotImplementedException();
}
