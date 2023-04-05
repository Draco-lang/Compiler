using System.Diagnostics;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Source;

internal sealed class SourceGlobalSymbol : GlobalSymbol
{
    public override Type Type
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            Debug.Assert(this.type is not null);
            return this.type;
        }
    }

    public override bool IsMutable => this.declaration.Syntax.Keyword.Kind == TokenKind.KeywordVar;
    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.declaration.Name;

    public override VariableDeclarationSyntax DeclarationSyntax => this.declaration.Syntax;

    public BoundExpression? Value
    {
        get
        {
            // NOTE: We check the TYPE here, as value is nullable,
            // but a type always needs to be inferred
            if (this.NeedsBuild) this.Build();
            return this.value;
        }
    }

    public override string Documentation => this.DeclarationSyntax.Documentation;

    // NOTE: We check the TYPE here, as value is nullable
    private bool NeedsBuild => this.type is null;

    private readonly GlobalDeclaration declaration;
    private Type? type;
    private BoundExpression? value;

    public SourceGlobalSymbol(Symbol? containingSymbol, GlobalDeclaration declaration)
    {
        this.ContainingSymbol = containingSymbol;
        this.declaration = declaration;
    }

    private void Build()
    {
        Debug.Assert(this.DeclaringCompilation is not null);
        var diagnostics = this.DeclaringCompilation.GlobalDiagnosticBag;

        var binder = this.DeclaringCompilation.GetBinder(this.DeclarationSyntax);
        var (type, value) = binder.BindGlobal(this, diagnostics);

        this.type = type;
        this.value = value;
    }
}
