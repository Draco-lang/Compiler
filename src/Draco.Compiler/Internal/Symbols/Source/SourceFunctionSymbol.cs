using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source function-definition.
/// </summary>
internal sealed class SourceFunctionSymbol : FunctionSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters => this.parameters ??= this.BuildParameters();
    private ImmutableArray<ParameterSymbol>? parameters;

    public override Type ReturnType => this.returnType ??= this.BuildReturnType();
    private Type? returnType;

    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.syntax.Name.Text;

    public BoundStatement Body => this.body ??= this.BuildBody();
    private BoundStatement? body;

    private readonly FunctionDeclarationSyntax syntax;

    public SourceFunctionSymbol(Symbol? containingSymbol, FunctionDeclarationSyntax syntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.syntax = syntax;
    }

    public SourceFunctionSymbol(Symbol? containingSymbol, FunctionDeclaration declaration)
        : this(containingSymbol, declaration.Syntax)
    {
    }

    private ImmutableArray<ParameterSymbol> BuildParameters() => this.syntax.ParameterList.Values
        .Select(this.BuildParameter)
        .ToImmutableArray();

    private ParameterSymbol BuildParameter(ParameterSyntax syntax) =>
        new SourceParameterSymbol(this, syntax);

    private Type BuildReturnType() => throw new System.NotImplementedException();

    private BoundStatement BuildBody()
    {
        Debug.Assert(this.DeclaringCompilation is not null);
        var binder = this.DeclaringCompilation.GetBinder(this.syntax.Body);
        return binder.BindFunctionBody(this.syntax.Body);
    }
}
