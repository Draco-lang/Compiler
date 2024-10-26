using System.Threading;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.FlowAnalysis;
using Draco.Compiler.Internal.Symbols.Syntax;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source defined field - either a global variable or a class member.
/// </summary>
internal sealed class SourceFieldSymbol(
    Symbol containingSymbol,
    VariableDeclarationSyntax syntax) : SyntaxFieldSymbol(containingSymbol, syntax), ISourceSymbol
{
    public override TypeSymbol Type => this.BindTypeAndValueIfNeeded(this.DeclaringCompilation!).Type;
    public BoundExpression? Value => this.BindTypeAndValueIfNeeded(this.DeclaringCompilation!).Value;

    // IMPORTANT: flag is type, needs to be written last
    // NOTE: We check the TYPE here, as value is nullable
    private bool NeedsBuild => Volatile.Read(ref this.type) is null;

    private TypeSymbol? type;
    private BoundExpression? value;

    private readonly object buildLock = new();

    public SourceFieldSymbol(Symbol containingSymbol, GlobalDeclaration declaration)
        : this(containingSymbol, declaration.Syntax)
    {
    }

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
}
