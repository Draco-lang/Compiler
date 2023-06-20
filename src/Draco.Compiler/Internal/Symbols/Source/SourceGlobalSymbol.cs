using System.Threading;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.FlowAnalysis;

namespace Draco.Compiler.Internal.Symbols.Source;

internal sealed class SourceGlobalSymbol : GlobalSymbol, ISourceSymbol
{
    public override TypeSymbol Type
    {
        get
        {
            if (!this.NeedsBuild) return this.type!;
            lock (this.buildLock)
            {
                if (this.NeedsBuild)
                {
                    var (type, value) = this.BindTypeAndValue(this.DeclaringCompilation!);
                    this.value = value;
                    // IMPORTANT: type is flag, written last
                    Volatile.Write(ref this.type, type);
                }
                return this.type!;
            }
        }
    }

    public override bool IsMutable => this.declaration.Syntax.Keyword.Kind == TokenKind.KeywordVar;
    public override Symbol ContainingSymbol { get; }
    public override string Name => this.declaration.Name;

    public override VariableDeclarationSyntax DeclaringSyntax => this.declaration.Syntax;

    public BoundExpression? Value
    {
        get
        {
            if (!this.NeedsBuild) return this.value;
            lock (this.buildLock)
            {
                // NOTE: We check the TYPE here, as value is nullable,
                // but a type always needs to be inferred
                if (this.NeedsBuild)
                {
                    var (type, value) = this.BindTypeAndValue(this.DeclaringCompilation!);
                    this.value = value;
                    // IMPORTANT: type is flag, written last
                    Volatile.Write(ref this.type, type);
                }
                return this.value;
            }
        }
    }

    public override string Documentation => this.DeclaringSyntax.Documentation;

    // IMPORTANT: flag is type, needs to be written last
    // NOTE: We check the TYPE here, as value is nullable
    private bool NeedsBuild => Volatile.Read(ref this.type) is null;

    private readonly GlobalDeclaration declaration;
    private TypeSymbol? type;
    private BoundExpression? value;

    private readonly object buildLock = new();

    public SourceGlobalSymbol(Symbol containingSymbol, GlobalDeclaration declaration)
    {
        this.ContainingSymbol = containingSymbol;
        this.declaration = declaration;
    }

    public void Bind(IBinderProvider binderProvider)
    {
        this.BindTypeAndValue(binderProvider);

        // Flow analysis
        if (this.Value is not null) DefiniteAssignment.Analyze(this.Value, binderProvider.DiagnosticBag);
        ValAssignment.Analyze(this, binderProvider.DiagnosticBag);
    }

    private (TypeSymbol Type, BoundExpression? Value) BindTypeAndValue(IBinderProvider binderProvider)
    {
        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        return binder.BindGlobal(this, binderProvider.DiagnosticBag);
    }
}
