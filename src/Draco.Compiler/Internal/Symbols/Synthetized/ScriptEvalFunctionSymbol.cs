using System.Collections.Immutable;
using System.Threading;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// Represents the evaluation function of a script.
/// </summary>
internal sealed class ScriptEvalFunctionSymbol(
    Symbol containingSymbol,
    ScriptEntrySyntax syntax) : FunctionSymbol, ISourceSymbol
{
    public override Symbol ContainingSymbol { get; } = containingSymbol;
    public override string Name => CompilerConstants.ScriptEntryPointName;
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;
    public override ScriptEntrySyntax DeclaringSyntax => syntax;

    public override ImmutableArray<ParameterSymbol> Parameters => [];

    public override BoundStatement Body => throw new System.NotImplementedException();
    public override TypeSymbol ReturnType => throw new System.NotImplementedException();

    // IMPORTANT: flag is returnType, needs to be written last
    private bool NeedsBuild => Volatile.Read(ref this.returnType) is null;

    private BoundStatement? body;
    private TypeSymbol? returnType;

    private readonly object buildLock = new();

    public void Bind(IBinderProvider binderProvider) => throw new System.NotImplementedException();

    private (BoundStatement Body, TypeSymbol ReturnType) BindTypeAndBodyIfNeeded(IBinderProvider binderProvider)
    {
        if (!this.NeedsBuild) return (this.body!, this.returnType!);
        lock (this.buildLock)
        {
            if (this.NeedsBuild) return (this.body!, this.returnType!);
            {
                var (body, returnType) = this.BindTypeAndBody(binderProvider);
                this.body = body;
                // IMPORTANT: flag is returnType, needs to be written last
                Volatile.Write(ref this.returnType, returnType);
            }
            return (this.body!, this.returnType!);
        }
    }

    private (BoundStatement Body, TypeSymbol? ReturnType) BindTypeAndBody(IBinderProvider binderProvider)
    {
        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        // TODO
        throw new System.NotImplementedException();
    }
}
