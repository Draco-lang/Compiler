using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source function-definition.
/// </summary>
internal sealed class SourceFunctionSymbol : FunctionSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters => throw new System.NotImplementedException();
    public override Type ReturnType => throw new System.NotImplementedException();
    public override Symbol? ContainingSymbol { get; }

    private readonly FunctionDeclaration declaration;

    public SourceFunctionSymbol(Symbol? containingSymbol, FunctionDeclaration declaration)
    {
        this.ContainingSymbol = containingSymbol;
        this.declaration = declaration;
    }
}
