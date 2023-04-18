using System.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.FlowAnalysis;

namespace Draco.Compiler.Internal.Symbols.Source;

internal sealed class SourceGlobalSymbol : GlobalSymbol, ISourceSymbol
{
    public override TypeSymbol Type
    {
        get
        {
            if (this.NeedsBuild)
            {
                var (type, value) = this.BindTypeAndValue(this.DeclaringCompilation!, this.DeclaringCompilation!.GlobalDiagnosticBag);
                this.type = type;
                this.value = value;
            }
            Debug.Assert(this.type is not null);
            return this.type;
        }
    }

    public override bool IsMutable => this.declaration.Syntax.Keyword.Kind == TokenKind.KeywordVar;
    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.declaration.Name;

    public override VariableDeclarationSyntax DeclaringSyntax => this.declaration.Syntax;

    public BoundExpression? Value
    {
        get
        {
            // NOTE: We check the TYPE here, as value is nullable,
            // but a type always needs to be inferred
            if (this.NeedsBuild)
            {
                var (type, value) = this.BindTypeAndValue(this.DeclaringCompilation!, this.DeclaringCompilation!.GlobalDiagnosticBag);
                this.type = type;
                this.value = value;
            }
            return this.value;
        }
    }

    public override string Documentation => this.DeclaringSyntax.Documentation;

    // NOTE: We check the TYPE here, as value is nullable
    private bool NeedsBuild => this.type is null;

    private readonly GlobalDeclaration declaration;
    private TypeSymbol? type;
    private BoundExpression? value;

    public SourceGlobalSymbol(Symbol? containingSymbol, GlobalDeclaration declaration)
    {
        this.ContainingSymbol = containingSymbol;
        this.declaration = declaration;
    }

    public void Bind(IBinderProvider binderProvider, DiagnosticBag diagnostics)
    {
        this.BindTypeAndValue(binderProvider, diagnostics);

        // Flow analysis
        if (this.Value is not null) DefiniteAssignment.Analyze(this.Value, diagnostics);
        ValAssignment.Analyze(this, diagnostics);
    }

    private (TypeSymbol Type, BoundExpression? Value) BindTypeAndValue(IBinderProvider binderProvider, DiagnosticBag diagnostics)
    {
        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        return binder.BindGlobal(this, diagnostics);
    }
}
