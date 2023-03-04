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
internal sealed class SourceParameterSymbol : ParameterSymbol, ISourceSymbol
{
    public override Type Type => this.type ??= this.BuildType();
    private Type? type;

    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DefinitionSyntax.Name.Text;

    public ParameterSyntax DefinitionSyntax { get; }
    SyntaxNode ISourceSymbol.DefinitionSyntax => this.DefinitionSyntax;

    public SourceParameterSymbol(Symbol? containingSymbol, ParameterSyntax syntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DefinitionSyntax = syntax;
    }

    private Type BuildType() => throw new System.NotImplementedException();
}
