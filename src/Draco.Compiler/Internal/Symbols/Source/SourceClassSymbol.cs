using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// Represents a class from source code.
/// </summary>
internal sealed class SourceClassSymbol : TypeSymbol
{
    public override Symbol ContainingSymbol { get; }

    public override ClassDeclarationSyntax DeclaringSyntax => this.declaration.Syntax;

    public override SymbolDocumentation Documentation => InterlockedUtils.InitializeNull(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    private readonly ClassDeclaration declaration;

    public SourceClassSymbol(Symbol containingSymbol, ClassDeclaration declaration)
    {
        this.ContainingSymbol = containingSymbol;
        this.declaration = declaration;
    }

    public override string ToString() => this.DeclaringSyntax.Name.Text;

    private SymbolDocumentation BuildDocumentation() =>
        MarkdownDocumentationExtractor.Extract(this);
}
