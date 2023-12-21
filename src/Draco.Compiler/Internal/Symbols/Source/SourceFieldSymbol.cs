using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// Represents a field from source code.
/// </summary>
internal sealed class SourceFieldSymbol : FieldSymbol, ISourceSymbol
{
    public override TypeSymbol ContainingSymbol { get; }

    public override TypeSymbol Type => this.BindTypeIfNeeded(this.DeclaringCompilation!);
    private TypeSymbol? type;

    public override string Name => this.DeclaringSyntax.Parameter.Name.Text;
    public override bool IsMutable => this.DeclaringSyntax.MemberModifiers!.Keyword.Kind == TokenKind.KeywordVar;
    public override Api.Semantics.Visibility Visibility => GetVisibilityFromTokenKind(this.DeclaringSyntax.MemberModifiers!.VisibilityModifier?.Kind);

    // TODO: This is not general, currently only works for fields declared in primary constructors
    public override PrimaryConstructorParameterSyntax DeclaringSyntax { get; }

    public SourceFieldSymbol(TypeSymbol containingSymbol, PrimaryConstructorParameterSyntax declaringSyntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclaringSyntax = declaringSyntax;
    }

    public void Bind(IBinderProvider binderProvider)
    {
        this.BindTypeIfNeeded(binderProvider);
    }

    private TypeSymbol BindTypeIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeNull(ref this.type, () => this.BindType(binderProvider));

    private TypeSymbol BindType(IBinderProvider binderProvider)
    {
        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        return binder.BindTypeToTypeSymbol(this.DeclaringSyntax.Parameter.Type, binderProvider.DiagnosticBag);
    }
}
