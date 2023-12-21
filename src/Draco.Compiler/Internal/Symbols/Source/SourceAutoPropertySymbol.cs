using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An auto-prop coming from source.
/// </summary>
internal sealed class SourceAutoPropertySymbol : PropertySymbol, ISourceSymbol
{
    public override TypeSymbol ContainingSymbol { get; }

    public override TypeSymbol Type => this.BindTypeIfNeeded(this.DeclaringCompilation!);
    private TypeSymbol? type;

    public override FunctionSymbol Getter => InterlockedUtils.InitializeNull(ref this.getter, this.BuildGetter);
    private FunctionSymbol? getter;

    public override FunctionSymbol? Setter => this.Modifiers.Keyword.Kind == TokenKind.KeywordVal
        ? null
        : InterlockedUtils.InitializeMaybeNull(ref this.setter, this.BuildSetter);
    private FunctionSymbol? setter;

    /// <summary>
    /// The backing field of this auto-prop.
    /// </summary>
    public FieldSymbol BackingField => InterlockedUtils.InitializeNull(ref this.backingField, this.BuildBackingField);
    private FieldSymbol? backingField;

    public override string Name => this.DeclaringSyntax.Parameter.Name.Text;
    public override bool IsIndexer => false;
    public override bool IsStatic => false;
    public override Api.Semantics.Visibility Visibility => GetVisibilityFromTokenKind(this.Modifiers.VisibilityModifier?.Kind);

    // TODO: Not necessarily this type, only for primary constructors
    public override PrimaryConstructorParameterSyntax DeclaringSyntax { get; }
    private PrimaryConstructorParameterMemberModifiersSyntax Modifiers => this.DeclaringSyntax.MemberModifiers!;

    public SourceAutoPropertySymbol(TypeSymbol containingSymbol, PrimaryConstructorParameterSyntax declaringSyntax)
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

    private FunctionSymbol BuildGetter() => new AutoPropertyGetterSymbol(this.ContainingSymbol, this);
    private FunctionSymbol? BuildSetter() => new AutoPropertySetterSymbol(this.ContainingSymbol, this);
    private FieldSymbol BuildBackingField() => new AutoPropertyBackingFieldSymbol(this.ContainingSymbol, this);
}
