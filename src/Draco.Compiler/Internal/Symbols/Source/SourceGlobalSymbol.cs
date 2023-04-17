using System.Diagnostics;
using Draco.Compiler.Api.Syntax;
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
            if (this.NeedsBuild) this.BindTypeAndValue(this.DeclaringCompilation!.GlobalDiagnosticBag);
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
            if (this.NeedsBuild) this.BindTypeAndValue(this.DeclaringCompilation!.GlobalDiagnosticBag);
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

    public void Bind(DiagnosticBag diagnostics)
    {
        this.BindTypeAndValue(diagnostics);

        // Flow analysis
        if (this.Value is not null) DefiniteAssignment.Analyze(this.Value, diagnostics);
        ValAssignment.Analyze(this, diagnostics);
    }

    private void BindTypeAndValue(DiagnosticBag diagnostics)
    {
        if (!this.NeedsBuild) return;

        Debug.Assert(this.DeclaringCompilation is not null);

        var binder = this.DeclaringCompilation.GetBinder(this.DeclaringSyntax);
        var (type, value) = binder.BindGlobal(this, diagnostics);

        this.type = type;
        this.value = value;
    }
}
