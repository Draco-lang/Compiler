using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Utilities;

namespace Draco.Compiler.Syntax;

/// <summary>
/// Constructs a <see cref="ParseTree"/>
/// </summary>
internal sealed class Parser
{
    /// <summary>
    /// The lexer we read token from
    /// </summary>
    public Lexer Lexer { get; set; }
    public Parser(Lexer lexer)
    {
        this.Lexer = lexer;
    }

    /// <summary>
    /// Constructs a <see cref="ParseTree"/> from tokens
    /// </summary>
    /// <returns>The <see cref="ParseTree.CompilationUnit"/> constructed</returns>
    public ParseTree Parse()
    {
        ValueArray<ParseTree.Decl> declarations = new ValueArray<ParseTree.Decl>();
        while (true)
        {
            var token = this.Lexer.Lex();
            if(token.Type == TokenType.EndOfInput)
                break;
        }
        return new ParseTree.CompilationUnit(declarations);
    }
}
