using System.Threading;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols.Syntax;
using Draco.Compiler.Internal.Symbols.Synthetized.AutoProperty;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// Auto-property defined based on some source.
/// Currently this class only models global =module-level) auto-properties, which is the equivalent of C# static auto-properties.
/// </summary>
internal sealed class SourceAutoPropertySymbol(
    Symbol containingSymbol,
    VariableDeclarationSyntax syntax) : SyntaxAutoPropertySymbol(containingSymbol, syntax)
{
    public override TypeSymbol Type => this.BindTypeIfNeeded(this.DeclaringCompilation!);
    private TypeSymbol? type;

    public override FunctionSymbol Getter => LazyInitializer.EnsureInitialized(ref this.getter, this.BuildGetter);
    private FunctionSymbol? getter;

    public override FunctionSymbol? Setter => this.DeclaringSyntax.Keyword.Kind == TokenKind.KeywordVal
        ? null
        : InterlockedUtils.InitializeMaybeNull(ref this.setter, this.BuildSetter);
    private FunctionSymbol? setter;

    /// <summary>
    /// The backing field of this auto-prop.
    /// </summary>
    public FieldSymbol BackingField => LazyInitializer.EnsureInitialized(ref this.backingField, this.BuildBackingField);
    private FieldSymbol? backingField;

    public override void Bind(IBinderProvider binderProvider) => this.BindTypeIfNeeded(binderProvider);

    private TypeSymbol BindTypeIfNeeded(IBinderProvider binderProvider) =>
        LazyInitializer.EnsureInitialized(ref this.type, () => this.BindType(binderProvider));

    private TypeSymbol BindType(IBinderProvider binderProvider)
    {
        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        // TODO: What if the type is null?
        if (this.DeclaringSyntax.Type is null) throw new System.NotImplementedException("TODO");
        return binder.BindTypeToTypeSymbol(this.DeclaringSyntax.Type.Type, binderProvider.DiagnosticBag);
    }

    private FunctionSymbol BuildGetter() => new AutoPropertyGetterSymbol(this.ContainingSymbol, this);
    private FunctionSymbol? BuildSetter() => new AutoPropertySetterSymbol(this.ContainingSymbol, this);
    private FieldSymbol BuildBackingField() => new AutoPropertyBackingFieldSymbol(this.ContainingSymbol, this);
}
