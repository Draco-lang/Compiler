using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Utilities;
using static Draco.Compiler.Syntax.ParseTree;

namespace Draco.Compiler.Syntax;

/// <summary>
/// Parses a sequence of <see cref="Token"/>s into a <see cref="ParseTree"/>.
/// </summary>
internal sealed class Parser
{
    private readonly ITokenSource tokenSource;

    public Parser(ITokenSource tokenSource)
    {
        this.tokenSource = tokenSource;
    }

    /// <summary>
    /// Parses a <see cref="CompilationUnit"/> until the end of input.
    /// </summary>
    /// <returns>The parsed <see cref="CompilationUnit"/>.</returns>
    public CompilationUnit ParseCompilationUnit()
    {
        var decls = ValueArray.CreateBuilder<Decl>();
        while (this.tokenSource.Peek().Type != TokenType.EndOfInput) decls.Add(this.ParseDeclaration());
        return new(decls.ToValue());
    }

    private Decl ParseDeclaration() => throw new NotImplementedException();
}
