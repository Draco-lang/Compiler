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
    /// <summary>
    /// Describes a precedence level.
    /// </summary>
    private enum PrecLevelKind
    {
        /// <summary>
        /// The level is for unary prefix operators.
        /// </summary>
        Prefix,

        /// <summary>
        /// The level is for binary left-associative operators.
        /// </summary>
        BinaryLeft,

        /// <summary>
        /// The level is for binary right-associative operators.
        /// </summary>
        BinaryRight,

        /// <summary>
        /// The level is completely custom with a custom parser function.
        /// </summary>
        Custom,
    }

    /// <summary>
    /// Describes a single precedence level for expressions.
    /// </summary>
    /// <param name="Kind">The kind of the precedence level.</param>
    /// <param name="Operators">The operator tokens for this level.</param>
    /// <param name="CustomParser">The custom parser for the level, if any.</param>
    private readonly record struct PrecLevel(
        PrecLevelKind Kind,
        TokenType[] Operators,
        Func<Parser, Func<Expr>, Expr> CustomParser)
    {
        /// <summary>
        /// Constructs a precedence level for prefix operators.
        /// </summary>
        /// <param name="ops">The valid prefix unary operator token types.</param>
        /// <returns>A precedence level descriptor.</returns>
        public static PrecLevel Prefix(params TokenType[] ops) => new(
            Kind: PrecLevelKind.Prefix,
            Operators: ops,
            CustomParser: (_1, _2) => throw new InvalidOperationException());

        /// <summary>
        /// Constructs a precedence level for binary left-associative operators.
        /// </summary>
        /// <param name="ops">The valid binary operator token types.</param>
        /// <returns>A precedence level descriptor.</returns>
        public static PrecLevel BinaryLeft(params TokenType[] ops) => new(
            Kind: PrecLevelKind.BinaryLeft,
            Operators: ops,
            CustomParser: (_1, _2) => throw new InvalidOperationException());

        /// <summary>
        /// Constructs a precedence level for binary right-associative operators.
        /// </summary>
        /// <param name="ops">The valid binary operator token types.</param>
        /// <returns>A precedence level descriptor.</returns>
        public static PrecLevel BinaryRight(params TokenType[] ops) => new(
            Kind: PrecLevelKind.BinaryRight,
            Operators: ops,
            CustomParser: (_1, _2) => throw new InvalidOperationException());

        /// <summary>
        /// Constructs a precedence level for a custom parser function.
        /// </summary>
        /// <param name="parserFunc">The parser function for the level.</param>
        /// <returns>A precedence level descriptor.</returns>
        public static PrecLevel Custom(Func<Parser, Func<Expr>, Expr> parserFunc) => new(
            Kind: PrecLevelKind.Custom,
            Operators: Array.Empty<TokenType>(),
            CustomParser: parserFunc);
    }

    /// <summary>
    /// The precedence table for the parser.
    /// Goes from highest precedence first, lowest precedence last.
    /// </summary>
    private static readonly PrecLevel[] precedenceTable = new[]
    {
        // Max precedence is atom
        PrecLevel.Custom((parser, _) => parser.ParseAtomExpr()),
        // Then comes call, indexing and member access
        PrecLevel.Custom((parser, subexprParser) => parser.ParseCallLevelExpr(subexprParser)),
        // Then prefix unary + and -
        PrecLevel.Prefix(TokenType.Plus, TokenType.Minus),
        // Then binary *, /, mod, rem
        PrecLevel.BinaryLeft(TokenType.Star, TokenType.Slash, TokenType.KeywordMod, TokenType.KeywordRem),
        // Then binary +, -
        PrecLevel.BinaryLeft(TokenType.Plus, TokenType.Minus),
        // Then relational operators
        PrecLevel.Custom((parser, subexprParser) => parser.ParseRelationalLevelExpr(subexprParser)),
        // Then unary not
        PrecLevel.Prefix(TokenType.KeywordNot),
        // Then binary and
        PrecLevel.BinaryLeft(TokenType.KeywordAnd),
        // Then binary or
        PrecLevel.BinaryLeft(TokenType.KeywordOr),
        // Then assignment and compound assignment, which are **RIGHT ASSOCIATIVE**
        PrecLevel.BinaryRight(
            TokenType.Assign,
            TokenType.PlusAssign, TokenType.MinusAssign,
            TokenType.StarAssign, TokenType.SlashAssign),
    };

    /// <summary>
    /// The list of all relational operators.
    /// </summary>
    private static readonly TokenType[] relationalOps = new[]
    {
        TokenType.Equal, TokenType.NotEqual,
        TokenType.GreaterThan, TokenType.LessThan,
        TokenType.GreaterEqual, TokenType.LessEqual,
    };

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
        while (this.Peek().Type != TokenType.EndOfInput) decls.Add(this.ParseDeclaration());
        return new(decls.ToValue());
    }

    /// <summary>
    /// Parses a declaration.
    /// </summary>
    /// <returns>The parsed <see cref="Decl"/>.</returns>
    private Decl ParseDeclaration()
    {
        var keyword = this.Peek();
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
    /// Parses a <see cref="Variable"/> declaration.
    /// </summary>
    /// <returns>The parsed <see cref="Variable"/>.</returns>
    private Variable ParseVariableDeclaration()
    {
        var keyword = this.Peek();
        if (keyword.Type == TokenType.KeywordVal || keyword.Type == TokenType.KeywordVar)
        {
            this.Advance();
        }
        var identifier = this.Expect(TokenType.Identifier);
        // We don't necessarily have type specifier
        TypeSpecifier? type = null;
        if (this.Peek().Type == TokenType.Colon)
        {
            var colon = this.Expect(TokenType.Colon);
            var typeIdentifier = this.Expect(TokenType.Identifier);
            type = new TypeSpecifier(colon, new TypeExpr.Name(typeIdentifier));
        }
        // We don't necessarily have value assigned to the variable
        (Token Assign, Expr Value)? assignment = null;
        if (this.Peek().Type == TokenType.Assign)
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
    /// Parsed a function declaration.
    /// </summary>
    /// <returns>The parsed <see cref="Func"/>.</returns>
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
            var token = this.Peek();
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
        if (this.Peek().Type == TokenType.Colon)
        {
            var colon = this.Expect(TokenType.Colon);
            var typeName = this.Expect(TokenType.Identifier);
            typeSpecifier = new TypeSpecifier(colon, new TypeExpr.Name(typeName));
        }
        FuncBody? body = null;
        // Inline function body
        if (this.Peek().Type == TokenType.Assign)
        {
            body = new FuncBody.InlineBody(this.Expect(TokenType.Assign), this.ParseExpr());
        }
        // Block function body
        else if (this.Peek().Type == TokenType.CurlyOpen)
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
        // The function that is driven by the precedence table
        Expr ParsePrecedenceLevel(int level)
        {
            var desc = precedenceTable[level];
            switch (desc.Kind)
            {
            case PrecLevelKind.Prefix:
            {
                var op = this.Peek();
                if (desc.Operators.Contains(op.Type))
                {
                    // There is such operator on this level
                    op = this.Advance();
                    var subexpr = ParsePrecedenceLevel(level);
                    return new Unary(op, subexpr);
                }
                // Just descent to next level
                return ParsePrecedenceLevel(level - 1);
            }
            case PrecLevelKind.BinaryLeft:
            {
                // We unroll left-associativity into a loop
                var result = ParsePrecedenceLevel(level - 1);
                while (true)
                {
                    var op = this.Peek();
                    if (!desc.Operators.Contains(op.Type)) break;
                    op = this.Advance();
                    var right = ParsePrecedenceLevel(level - 1);
                    result = new Binary(result, op, right);
                }
                return result;
            }
            case PrecLevelKind.BinaryRight:
            {
                // Right-associativity is simply right-recursion
                var result = ParsePrecedenceLevel(level - 1);
                var op = this.Peek();
                if (desc.Operators.Contains(op.Type))
                {
                    op = this.Advance();
                    var right = ParsePrecedenceLevel(level);
                    result = new Binary(result, op, right);
                }
                return result;
            }
            case PrecLevelKind.Custom:
                // Just call the custom parser
                return desc.CustomParser(this, () => ParsePrecedenceLevel(level - 1));
            default:
                throw new InvalidOperationException("no such precedence level kind");
            }
        }

        return ParsePrecedenceLevel(precedenceTable.Length - 1);
    }

    private Expr ParseRelationalLevelExpr(Func<Expr> elementParser)
    {
        var left = elementParser();
        var comparisons = ValueArray.CreateBuilder<(Token Operator, Expr Right)>();
        while (true)
        {
            var op = this.Peek();
            if (!relationalOps.Contains(op.Type)) break;
            op = this.Advance();
            var right = elementParser();
            comparisons.Add((op, right));
        }
        return comparisons.Count == 0
            ? left
            : new Relational(left, comparisons.ToValue());
    }

    private Expr ParseCallLevelExpr(Func<Expr> elementParser)
    {
        var result = elementParser();
        while (true)
        {
            var peek = this.Peek();
            if (peek.Type == TokenType.ParenOpen)
            {
                var args = this.ParseEnclosed(
                    openType: TokenType.ParenOpen,
                    valueParser: () => this.ParsePunctuatedListAllowTrailing(
                        elementParser: this.ParseExpr,
                        punctType: TokenType.Comma,
                        stopType: TokenType.ParenClose,
                        allowEmpty: true),
                    closeType: TokenType.ParenClose);
                result = new Call(result, args);
            }
            else if (peek.Type == TokenType.BracketOpen)
            {
                var args = this.ParseEnclosed(
                    openType: TokenType.BracketOpen,
                    valueParser: () => this.ParsePunctuatedListAllowTrailing(
                        elementParser: this.ParseExpr,
                        punctType: TokenType.Comma,
                        stopType: TokenType.BracketClose,
                        allowEmpty: false),
                    closeType: TokenType.BracketClose);
                result = new Call(result, args);
            }
            else
            {
                break;
            }
        }
        return result;
    }

    private Expr ParseAtomExpr()
    {
        var peek = this.Peek();
        switch (peek.Type)
        {
        case TokenType.LiteralInteger:
        {
            var value = this.Advance();
            return new Literal(value);
        }
        case TokenType.Identifier:
        {
            var name = this.Advance();
            return new Name(name);
        }
        default:
            // TODO
            throw new NotImplementedException();
        }
    }

    // General utilities

    private PunctuatedList<T> ParsePunctuatedListAllowTrailing<T>(
        Func<T> elementParser,
        TokenType punctType,
        TokenType stopType,
        bool allowEmpty)
    {
        var elements = ValueArray.CreateBuilder<Punctuated<T>>();
        while (true)
        {
            // Stop token met, don't go further
            if (this.Peek().Type == stopType) break;
            // Parse an element
            var element = elementParser();
            // If the next token is not a punctuation, we are done
            var punct = this.Peek();
            if (punct.Type == punctType)
            {
                // Punctuation, add with element
                punct = this.Advance();
                elements.Add(new(element, punct));
            }
            else
            {
                // Not punctuation, we are done
                elements.Add(new(element, null));
                break;
            }
        }
        if (!allowEmpty && elements.Count == 0) throw new NotImplementedException();
        return new(elements.ToValue());
    }

    private Enclosed<T> ParseEnclosed<T>(TokenType openType, Func<T> valueParser, TokenType closeType)
    {
        var openToken = this.Expect(openType);
        var value = valueParser();
        var closeToken = this.Expect(closeType);
        return new(openToken, value, closeToken);
    }

    // Token-level operators

    private Token Expect(TokenType type)
    {
        var token = this.tokenSource.Peek();
        if (token.Type != type) throw new NotImplementedException();

        return this.Advance();
    }

    private Token Peek(int offset = 0) => this.tokenSource.Peek(offset);

    private Token Advance()
    {
        var token = this.Peek();
        this.tokenSource.Advance();
        return token;
    }
}
