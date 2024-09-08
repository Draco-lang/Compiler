using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols.Source;
internal class SourceFieldSymbol : FieldSymbol, ISourceSymbol
{
    public override TypeSymbol Type => this.BindTypeIfNeeded(this.DeclaringCompilation);
    private TypeSymbol? type;

    public override bool IsMutable => this.DeclaringSyntax.Keyword.Kind == TokenKind.KeywordVar;

    public override VariableDeclarationSyntax DeclaringSyntax { get; }
}
