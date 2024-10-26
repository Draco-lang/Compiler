using System.Threading;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.FlowAnalysis;
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
    public override TypeSymbol Type => this.BindTypeAndValueIfNeeded(this.DeclaringCompilation!).Type;
    public BoundExpression? Value => this.BindTypeAndValueIfNeeded(this.DeclaringCompilation!).Value;

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

    // IMPORTANT: flag is type, needs to be written last
    // NOTE: We check the TYPE here, as value is nullable
    private bool NeedsBuild => Volatile.Read(ref this.type) is null;

    private TypeSymbol? type;
    private BoundExpression? value;

    private readonly object buildLock = new();

    public override void Bind(IBinderProvider binderProvider)
    {
        this.BindTypeAndValueIfNeeded(binderProvider);

        // Flow analysis
        CompleteFlowAnalysis.AnalyzeValue(this, binderProvider.DiagnosticBag);
    }

    private (TypeSymbol Type, BoundExpression? Value) BindTypeAndValueIfNeeded(IBinderProvider binderProvider)
    {
        if (!this.NeedsBuild) return (this.type!, this.value);
        lock (this.buildLock)
        {
            // NOTE: We check the TYPE here, as value is nullable,
            // but a type always needs to be inferred
            if (this.NeedsBuild)
            {
                var (type, value) = this.BindTypeAndValue(binderProvider);
                this.value = value;
                // IMPORTANT: type is flag, written last
                Volatile.Write(ref this.type, type);
            }
            return (this.type!, this.value);
        }
    }

    private GlobalBinding BindTypeAndValue(IBinderProvider binderProvider)
    {
        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        return binder.BindGlobal(this, binderProvider.DiagnosticBag);
    }

    private FunctionSymbol BuildGetter() => new AutoPropertyGetterSymbol(this.ContainingSymbol, this);
    private FunctionSymbol? BuildSetter() => new AutoPropertySetterSymbol(this.ContainingSymbol, this);
    private FieldSymbol BuildBackingField() => new AutoPropertyBackingFieldSymbol(this.ContainingSymbol, this);
}
