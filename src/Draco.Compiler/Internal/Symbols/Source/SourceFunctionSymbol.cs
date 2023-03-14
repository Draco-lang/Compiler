using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source function-definition.
/// </summary>
internal sealed class SourceFunctionSymbol : FunctionSymbol, ISourceSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters => this.parameters ??= this.BuildParameters();
    private ImmutableArray<ParameterSymbol>? parameters;

    public override Type ReturnType => this.returnType ??= this.BuildReturnType();
    private Type? returnType;

    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DeclarationSyntax.Name.Text;

    /// <summary>
    /// The syntax the symbol was constructed from.
    /// </summary>
    public FunctionDeclarationSyntax DeclarationSyntax { get; }
    SyntaxNode ISourceSymbol.DeclarationSyntax => this.DeclarationSyntax;

    public BoundStatement Body => this.body ??= this.BuildBody();
    private BoundStatement? body;

    public SourceFunctionSymbol(Symbol? containingSymbol, FunctionDeclarationSyntax syntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclarationSyntax = syntax;
    }

    public SourceFunctionSymbol(Symbol? containingSymbol, FunctionDeclaration declaration)
        : this(containingSymbol, declaration.Syntax)
    {
    }

    public override ISymbol ToApiSymbol() => new Api.Semantics.FunctionSymbol(this);

    private ImmutableArray<ParameterSymbol> BuildParameters() => this.DeclarationSyntax.ParameterList.Values
        .Select(this.BuildParameter)
        .ToImmutableArray();

    private ParameterSymbol BuildParameter(ParameterSyntax syntax) =>
        new SourceParameterSymbol(this, syntax);

    private Type BuildReturnType()
    {
        // If the return type is unspecified, it's assumed to be unit
        if (this.DeclarationSyntax.ReturnType is null) return Intrinsics.Unit;

        // Otherwise, we need to resolve
        Debug.Assert(this.DeclaringCompilation is not null);
        // TODO: These are not exposed
        var constraints = new ConstraintBag();
        var diagnostics = new DiagnosticBag();
        var binder = this.DeclaringCompilation.GetBinder(this.DeclarationSyntax.Body);
        var returnTypeSymbol = binder.BindType(this.DeclarationSyntax.ReturnType.Type, constraints, diagnostics);
        return ((TypeSymbol)returnTypeSymbol).Type;
    }

    private BoundStatement BuildBody()
    {
        Debug.Assert(this.DeclaringCompilation is not null);
        var binder = this.DeclaringCompilation.GetBinder(this.DeclarationSyntax.Body);
        return binder.BindFunctionBody(this.DeclarationSyntax.Body);
    }
}
