using System;
using System.Threading;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Internal.Symbols.Source;
internal class SourceFieldSymbol(TypeSymbol containingSymbol, VariableDeclarationSyntax field) : FieldSymbol, ISourceSymbol
{
    public override TypeSymbol ContainingSymbol => containingSymbol;

    public override TypeSymbol Type => this.BindTypeIfNeeded(this.DeclaringCompilation!);

    public override Api.Semantics.Visibility Visibility =>
        GetVisibilityFromTokenKind(this.DeclaringSyntax.VisibilityModifier?.Kind);

    private TypeSymbol BindTypeIfNeeded(IBinderProvider binder) =>
        LazyInitializer.EnsureInitialized(ref this.type, () => this.BindType(binder));

    private TypeSymbol? type;

    public override bool IsMutable => this.DeclaringSyntax.Keyword.Kind == TokenKind.KeywordVar;

    public override VariableDeclarationSyntax DeclaringSyntax { get; } = field;

    private TypeSymbol BindType(IBinderProvider binderProvider)
    {
        if (this.DeclaringSyntax.Type is null) throw new NotImplementedException(); // TODO: what should I do when the type is missing? Do we allow inference here ?

        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        return binder.BindTypeToTypeSymbol(this.DeclaringSyntax.Type.Type, binderProvider.DiagnosticBag);
    }

    public void Bind(IBinderProvider binder) =>
        this.BindTypeIfNeeded(binder);
}
