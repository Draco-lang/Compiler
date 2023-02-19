using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A function parameter defined in-source.
/// </summary>
internal sealed class SourceParameterSymbol : ParameterSymbol
{
    public override Type Type => throw new System.NotImplementedException();
    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.syntax.Name.Text;

    private readonly ParameterSyntax syntax;

    public SourceParameterSymbol(Symbol? containingSymbol, ParameterSyntax syntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.syntax = syntax;
    }
}
