using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Utilities;
using static Draco.Compiler.Syntax.ParseTree;
using static Draco.Compiler.Syntax.ParseTree.Expr;
using static Draco.Compiler.Syntax.ParseTree.Decl;

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

    /// <summary>
    /// Function for parsing Declaration.
    /// </summary>
    /// <returns>Parsed Declaration.</returns>
    private Decl ParseDeclaration()
    {
        var keyword = this.tokenSource.Peek();
        if (keyword.Type == TokenType.KeywordFunc)
        {
            return this.ParseFuncDeclaration();
        }
        else if (keyword.Type == TokenType.KeywordVar || keyword.Type == TokenType.KeywordVal)
        {
            return this.ParseVariableDeclaration();
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Function for parsing <see cref="Variable"/> declarations.
    /// </summary>
    /// <returns>Parsed <see cref="Variable"/>.</returns>
    private Decl.Variable ParseVariableDeclaration()
    {
        var keyword = this.tokenSource.Peek();
        if (keyword.Type == TokenType.KeywordVal || keyword.Type == TokenType.KeywordVar)
        {
            this.tokenSource.Advance();
        }
        var identifier = this.Expect(TokenType.Identifier);
        // We don't necessarily have type specifier
        TypeSpecifier? type = null;
        if (this.tokenSource.Peek().Type == TokenType.Colon)
        {
            var colon = this.Expect(TokenType.Colon);
            var typeIdentifier = this.Expect(TokenType.Identifier);
            type = new TypeSpecifier(colon, new TypeExpr.Name(typeIdentifier));
        }
        // We don't necessarily have value assigned to the variable
        (Token Assign, Expr Value)? assignment = null;
        if (this.tokenSource.Peek().Type == TokenType.Assign)
        {
            var assign = this.Expect(TokenType.Assign);
            var value = this.ParseExpr();
            assignment = (assign, value);
        }
        // Eat semicolon at the end of declaration
        this.Expect(TokenType.Semicolon);
        return new Decl.Variable(keyword, identifier, type, assignment);
    }

    /// <summary>
    /// Function for parsing Function declarations.
    /// </summary>
    /// <returns>Parsed Function declaration.</returns>
    private Func ParseFuncDeclaration()
    {
        // Func keyword and name of the function
        var funcKeyword = this.Expect(TokenType.KeywordFunc);
        var name = this.Expect(TokenType.Identifier);

        // Parameters
        var openParen = this.Expect(TokenType.ParenOpen);
        ValueArray<Punctuated<FuncParam>>.Builder funcParams = ValueArray.CreateBuilder<Punctuated<FuncParam>>();
        while (true)
        {
            var token = this.tokenSource.Peek();
            if (token.Type == TokenType.ParenClose) break;
            var paramID = this.Expect(TokenType.Identifier);
            var colon = this.Expect(TokenType.Colon);
            var paramType = this.Expect(TokenType.Identifier);
            // TODO: trailing comma is optional!
            var punctation = this.Expect(TokenType.Comma);
            funcParams.Add(new(new FuncParam(
                paramID,
                new TypeSpecifier(colon, new TypeExpr.Name(paramType))), punctation));
        }
        var closeParen = this.Expect(TokenType.ParenClose);
        var funcParameters = new Enclosed<PunctuatedList<FuncParam>>(openParen, new PunctuatedList<FuncParam>(funcParams.ToValue()), closeParen);

        // We don't necessarily have type specifier
        TypeSpecifier? typeSpecifier = null;
        if (this.tokenSource.Peek().Type == TokenType.Colon)
        {
            var colon = this.Expect(TokenType.Colon);
            var typeName = this.Expect(TokenType.Identifier);
            typeSpecifier = new TypeSpecifier(colon, new TypeExpr.Name(typeName));
        }
        FuncBody? body = null;
        // Inline function body
        if (this.tokenSource.Peek().Type == TokenType.Assign)
        {
            body = new FuncBody.InlineBody(this.Expect(TokenType.Assign), this.ParseExpr());
        }
        // Block function body
        else if (this.tokenSource.Peek().Type == TokenType.CurlyOpen)
        {
            body = new FuncBody.BlockBody(this.ParseBlock());
        }
        else
        {
            throw new NotImplementedException();
        }
        return new Func(funcKeyword, name, funcParameters, typeSpecifier, body);
    }

    private Block ParseBlock()
    {
        throw new NotImplementedException();
    }

    private Expr ParseExpr()
    {
        throw new NotImplementedException();
    }

    private Token Expect(TokenType type)
    {
        var token = this.tokenSource.Peek();
        if (token.Type != type) throw new NotImplementedException();

        this.tokenSource.Advance();
        return token;
    }
}
