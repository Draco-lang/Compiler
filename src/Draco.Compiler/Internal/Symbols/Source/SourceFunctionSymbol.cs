using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
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

    public override Type ReturnType => throw new System.NotImplementedException();
    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.declaration.Name;

    private readonly FunctionDeclaration declaration;

    public SourceFunctionSymbol(Symbol? containingSymbol, FunctionDeclaration declaration)
    {
        this.ContainingSymbol = containingSymbol;
        this.declaration = declaration;
    }

    private ImmutableArray<ParameterSymbol> BuildParameters() => this.declaration.Syntax.ParameterList.Values
        .Select(this.BuildParameter)
        .ToImmutableArray();

    private ParameterSymbol BuildParameter(ParameterSyntax syntax) =>
        new SourceParameterSymbol(this, syntax);
}
