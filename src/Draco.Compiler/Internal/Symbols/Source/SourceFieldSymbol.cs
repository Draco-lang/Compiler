using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Internal.Symbols.Source;
internal class SourceFieldSymbol(FunctionSymbol containingSymbol, FieldDeclarationSyntax field) : FieldSymbol, ISourceSymbol
{
    public override Symbol? ContainingSymbol => containingSymbol;

    public override TypeSymbol Type => this.BindTypeIfNeeded(this.DeclaringCompilation!);

    public override Api.Semantics.Visibility Visibility =>
        GetVisibilityFromTokenKind(this.DeclaringSyntax.VisibilityModifier);

    private TypeSymbol BindTypeIfNeeded(Compilation declaringCompilation) =>
        LazyInitializer.EnsureInitialized(ref this.type, () => this.BindType(declaringCompilation));

    private TypeSymbol? type;

    public override bool IsMutable => this.DeclaringSyntax.Keyword.Kind == TokenKind.KeywordVar;

    public override FieldDeclarationSyntax DeclaringSyntax { get; } = field;

    private TypeSymbol BindType(IBinderProvider binderProvider)
    {
        if (this.DeclaringSyntax.Type is null) throw new NotImplementedException(); // TODO: what should I do when the type is missing? Do we allow inference here ?

        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        return binder.BindTypeToTypeSymbol(this.DeclaringSyntax.Type.Type, binderProvider.DiagnosticBag);
    }
}
