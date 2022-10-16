using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Diagnostics;
using Draco.Compiler.Utilities;
using static Draco.Compiler.Syntax.ParseTree;

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
    /// Control flow statements parse sligtly differently in expression and statement contexts.
    /// This is the discriminating enum for them to avoid duplicating parser code.
    /// </summary>
    private enum ControlFlowContext
    {
        /// <summary>
        /// Control flow for an expression.
        /// </summary>
        Expr,

        /// <summary>
        /// Control flow for a statement.
        /// </summary>
        Stmt,
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
        // Finally the pseudo-statement-like constructs
        PrecLevel.Custom((parser, subexprParser) => parser.ParsePseudoStmtLevelExpr(subexprParser)),
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

    /// <summary>
    /// The list of all tokens that can start an expression.
    /// </summary>
    private static readonly TokenType[] expressionStarters = new[]
    {
        TokenType.Identifier,
        TokenType.LiteralInteger,
        TokenType.LiteralCharacter,
        TokenType.LineStringStart,
        TokenType.MultiLineStringStart,
        TokenType.KeywordFalse,
        TokenType.KeywordFunc,
        TokenType.KeywordGoto,
        TokenType.KeywordIf,
        TokenType.KeywordNot,
        TokenType.KeywordReturn,
        TokenType.KeywordTrue,
        TokenType.KeywordWhile,
        TokenType.ParenOpen,
        TokenType.CurlyOpen,
        TokenType.BracketOpen,
        TokenType.Plus,
        TokenType.Minus,
        TokenType.Star,
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
        switch (this.Peek().Type)
        {
        case TokenType.KeywordFunc:
            return this.ParseFuncDeclaration();

        case TokenType.KeywordVar:
        case TokenType.KeywordVal:
            return this.ParseVariableDeclaration();

        case TokenType.Identifier when this.Peek(1).Type == TokenType.Colon:
            return this.ParseLabelDeclaration();

        default:
        {
            // We consume as much bogus input as it makes sense
            // NOTE: We don't advance here in case the upcoming token should not be consumed for synchronization
            var input = ValueArray.CreateBuilder<Token>();
            while (true)
            {
                switch (this.Peek().Type)
                {
                case TokenType.EndOfInput:
                case TokenType.Semicolon:
                case TokenType.CurlyClose:
                case TokenType.KeywordFunc:
                case TokenType.KeywordVar:
                case TokenType.KeywordVal:
                case TokenType.Identifier when this.Peek(1).Type == TokenType.Colon:
                    goto end_of_error;

                default:
                    input.Add(this.Advance());
                    break;
                }
            }
        end_of_error:
            var location = new Location(0);
            var diag = Diagnostic.Create(SyntaxErrors.UnexpectedInput, location, formatArgs: "declaration");
            return new Decl.Unexpected(input.ToValue(), ValueArray.Create(diag));
        }
        }
    }

    /// <summary>
    /// Parses a statement.
    /// </summary>
    /// <returns>The parsed <see cref="Stmt"/>.</returns>
    private Stmt ParseStatement(bool allowDecl)
    {
        switch (this.Peek().Type)
        {
        // Declarations
        case TokenType.KeywordFunc when allowDecl:
        case TokenType.KeywordVar when allowDecl:
        case TokenType.KeywordVal when allowDecl:
        case TokenType.Identifier when allowDecl && this.Peek(1).Type == TokenType.Colon:
        {
            var decl = this.ParseDeclaration();
            return new Stmt.Decl(decl);
        }

        // Expressions that can appear without braces
        case TokenType.CurlyOpen:
        case TokenType.KeywordIf:
        case TokenType.KeywordWhile:
        {
            var expr = this.ParseControlFlowExpr(ControlFlowContext.Stmt);
            return new Stmt.Expr(expr, null);
        }

        // Assume expression
        default:
        {
            // TODO: This assumption might not be the best
            var expr = this.ParseExpr();
            var semicolon = this.Expect(TokenType.Semicolon);
            return new Stmt.Expr(expr, semicolon);
        }
        }
    }

    /// <summary>
    /// Parses a <see cref="Decl.Variable"/>.
    /// </summary>
    /// <returns>The parsed <see cref="Decl.Variable"/>.</returns>
    private Decl.Variable ParseVariableDeclaration()
    {
        var keyword = this.Peek();
        // NOTE: We will always call this function by checking the leading keyword
        Debug.Assert(keyword.Type == TokenType.KeywordVal || keyword.Type == TokenType.KeywordVar);
        keyword = this.Advance();
        var identifier = this.Expect(TokenType.Identifier);
        // We don't necessarily have type specifier
        TypeSpecifier? type = null;
        if (this.Peek().Type == TokenType.Colon) type = this.ParseTypeSpecifier();
        // We don't necessarily have value assigned to the variable
        (Token Assign, Expr Value)? assignment = null;
        if (this.Matches(TokenType.Assign, out var assign))
        {
            var value = this.ParseExpr();
            assignment = (assign, value);
        }
        // Eat semicolon at the end of declaration
        var semicolon = this.Expect(TokenType.Semicolon);
        return new Decl.Variable(keyword, identifier, type, assignment, semicolon);
    }

    /// <summary>
    /// Parsed a function declaration.
    /// </summary>
    /// <returns>The parsed <see cref="Func"/>.</returns>
    private Decl.Func ParseFuncDeclaration()
    {
        // Func keyword and name of the function
        var funcKeyword = this.Expect(TokenType.KeywordFunc);
        var name = this.Expect(TokenType.Identifier);

        // Parameters
        var funcParameters = this.ParseEnclosed(
            openType: TokenType.ParenOpen,
            valueParser: () => this.ParsePunctuatedListAllowTrailing(
                elementParser: this.ParseFuncParam,
                punctType: TokenType.Comma,
                stopType: TokenType.ParenClose),
            closeType: TokenType.ParenClose);

        // We don't necessarily have type specifier
        TypeSpecifier? returnType = null;
        if (this.Peek().Type == TokenType.Colon) returnType = this.ParseTypeSpecifier();

        var body = this.ParseFuncBody();

        return new Decl.Func(funcKeyword, name, funcParameters, returnType, body);
    }

    private Decl.Label ParseLabelDeclaration()
    {
        var labelName = this.Expect(TokenType.Identifier);
        var colon = this.Expect(TokenType.Colon);
        return new(labelName, colon);
    }

    private FuncParam ParseFuncParam()
    {
        var name = this.Expect(TokenType.Identifier);
        var typeSpec = this.ParseTypeSpecifier();
        return new(name, typeSpec);
    }

    private FuncBody ParseFuncBody()
    {
        if (this.Matches(TokenType.Assign, out var assign))
        {
            var expr = this.ParseExpr();
            var semicolon = this.Expect(TokenType.Semicolon);
            return new FuncBody.InlineBody(assign, expr, semicolon);
        }
        else if (this.Peek().Type == TokenType.CurlyOpen)
        {
            var block = this.ParseBlockExpr(ControlFlowContext.Stmt);
            return new FuncBody.BlockBody(block);
        }
        else
        {
            // TODO: Error handling
            // Expected a function body staring with '=' or '{'
            throw new NotImplementedException("expected function body");
        }
    }

    private TypeSpecifier ParseTypeSpecifier()
    {
        var colon = this.Expect(TokenType.Colon);
        var type = this.ParseTypeExpr();
        return new(colon, type);
    }

    private TypeExpr ParseTypeExpr()
    {
        // For now we only allow identifiers
        var typeName = this.Expect(TokenType.Identifier);
        return new TypeExpr.Name(typeName);
    }

    private Expr ParseControlFlowExpr(ControlFlowContext ctx)
    {
        var peekType = this.Peek().Type;
        Debug.Assert(peekType == TokenType.CurlyOpen
                  || peekType == TokenType.KeywordIf
                  || peekType == TokenType.KeywordWhile);
        return peekType switch
        {
            TokenType.CurlyOpen => this.ParseBlockExpr(ctx),
            TokenType.KeywordIf => this.ParseIfExpr(ctx),
            TokenType.KeywordWhile => this.ParseWhileExpr(ctx),
            _ => throw new InvalidOperationException(),
        };
    }

    private Expr ParseControlFlowBody(ControlFlowContext ctx)
    {
        if (ctx == ControlFlowContext.Expr)
        {
            // Only expressions, no semicolon needed
            return this.ParseExpr();
        }
        else
        {
            // Just a statement
            // Since this is a one-liner, we don't allow declarations as for example
            // if (x) var y = z;
            // makes no sense!
            var stmt = this.ParseStatement(allowDecl: false);
            return new Expr.UnitStmt(stmt);
        }
    }

    private Expr.Block ParseBlockExpr(ControlFlowContext ctx)
    {
        var enclosed = this.ParseEnclosed(
            openType: TokenType.CurlyOpen,
            valueParser: () =>
            {
                var stmts = ValueArray.CreateBuilder<Stmt>();
                Expr? value = null;
                while (true)
                {
                    switch (this.Peek().Type)
                    {
                    case TokenType.CurlyClose:
                        // On a close curly we can immediately exit
                        goto end_of_block;

                    case TokenType.KeywordFunc:
                    case TokenType.KeywordVar:
                    case TokenType.KeywordVal:
                    case TokenType.Identifier when this.Peek(1).Type == TokenType.Colon:
                    {
                        var decl = this.ParseDeclaration();
                        stmts.Add(new Stmt.Decl(decl));
                        break;
                    }

                    case TokenType.CurlyOpen:
                    case TokenType.KeywordIf:
                    case TokenType.KeywordWhile:
                    {
                        var expr = this.ParseControlFlowExpr(ctx);
                        if (this.Peek().Type == TokenType.CurlyClose)
                        {
                            // Treat as expression
                            value = expr;
                            goto end_of_block;
                        }
                        // Just a statement
                        stmts.Add(new Stmt.Expr(expr, null));
                        break;
                    }

                    default:
                    {
                        // Assume any other expression
                        // TODO: Might not be the best assumption
                        var expr = this.ParseExpr();
                        if (this.Matches(TokenType.Semicolon, out var semicolon))
                        {
                            // Just a statement, can continue
                            stmts.Add(new Stmt.Expr(expr, semicolon));
                        }
                        else
                        {
                            // This is the value of the block
                            value = expr;
                            goto end_of_block;
                        }
                        break;
                    }
                    }
                }
            end_of_block:
                return (stmts.ToValue(), value);
            },
            closeType: TokenType.CurlyClose);
        return new(enclosed);
    }

    private Expr.If ParseIfExpr(ControlFlowContext ctx)
    {
        var ifKeyword = this.Expect(TokenType.KeywordIf);
        var condition = this.ParseEnclosed(
            openType: TokenType.ParenOpen,
            valueParser: this.ParseExpr,
            closeType: TokenType.ParenClose);
        var thenBody = this.ParseControlFlowBody(ctx);

        (Token ElseKeyword, Expr Value)? elsePart = null;
        if (this.Matches(TokenType.KeywordElse, out var elseKeyword))
        {
            var elseBody = this.ParseControlFlowBody(ctx);
            elsePart = (elseKeyword, elseBody);
        }

        return new(ifKeyword, condition, thenBody, elsePart);
    }

    private Expr.While ParseWhileExpr(ControlFlowContext ctx)
    {
        var whileKeyword = this.Expect(TokenType.KeywordWhile);
        var condition = this.ParseEnclosed(
            openType: TokenType.ParenOpen,
            valueParser: this.ParseExpr,
            closeType: TokenType.ParenClose);
        var body = this.ParseControlFlowBody(ctx);
        return new(whileKeyword, condition, body);
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
                    return new Expr.Unary(op, subexpr);
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
                    result = new Expr.Binary(result, op, right);
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
                    result = new Expr.Binary(result, op, right);
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

    private Expr ParsePseudoStmtLevelExpr(Func<Expr> elementParser)
    {
        switch (this.Peek().Type)
        {
        case TokenType.KeywordReturn:
        {
            var returnKeyword = this.Advance();
            Expr? value = null;
            if (expressionStarters.Contains(this.Peek().Type)) value = this.ParseExpr();
            return new Expr.Return(returnKeyword, value);
        }

        case TokenType.KeywordGoto:
        {
            var gotoKeyword = this.Advance();
            var labelName = this.Expect(TokenType.Identifier);
            return new Expr.Goto(gotoKeyword, labelName);
        }

        default:
            return elementParser();
        }
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
            : new Expr.Relational(left, comparisons.ToValue());
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
                        stopType: TokenType.ParenClose),
                    closeType: TokenType.ParenClose);
                result = new Expr.Call(result, args);
            }
            else if (peek.Type == TokenType.BracketOpen)
            {
                var args = this.ParseEnclosed(
                    openType: TokenType.BracketOpen,
                    valueParser: () => this.ParsePunctuatedListAllowTrailing(
                        elementParser: this.ParseExpr,
                        punctType: TokenType.Comma,
                        stopType: TokenType.BracketClose),
                    closeType: TokenType.BracketClose);
                result = new Expr.Call(result, args);
            }
            else if (this.Matches(TokenType.Dot, out var dot))
            {
                var name = this.Expect(TokenType.Identifier);
                result = new Expr.MemberAccess(result, dot, name);
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
            return new Expr.Literal(value);
        }
        case TokenType.KeywordTrue:
        case TokenType.KeywordFalse:
        {
            var value = this.Advance();
            return new Expr.Literal(value);
        }
        case TokenType.Identifier:
        {
            var name = this.Advance();
            return new Expr.Name(name);
        }
        case TokenType.ParenOpen:
        {
            var content = this.ParseEnclosed(TokenType.ParenOpen, this.ParseExpr, TokenType.ParenClose);
            return new Expr.Grouping(content);
        }
        case TokenType.CurlyOpen:
        case TokenType.KeywordIf:
        case TokenType.KeywordWhile:
            return this.ParseControlFlowExpr(ControlFlowContext.Expr);
        default:
        {
            // NOTE: We don't advance here in case the upcoming token should not be consumed for synchronization
            // We consume as much bogus input as it makes sense
            var input = ValueArray.CreateBuilder<Token>();
            while (true)
            {
                switch (this.Peek().Type)
                {
                case TokenType.EndOfInput:
                case TokenType.CurlyClose:
                case TokenType.BracketClose:
                case TokenType.ParenClose:
                case TokenType.Semicolon:
                case var type when expressionStarters.Contains(type):
                    goto end_of_error;

                default:
                    input.Add(this.Advance());
                    break;
                }
            }
        end_of_error:
            var location = new Location(0);
            var diag = Diagnostic.Create(SyntaxErrors.UnexpectedInput, location, formatArgs: "expression");
            return new Expr.Unexpected(input.ToValue(), ValueArray.Create(diag));
        }
        }
    }

    // General utilities

    /// <summary>
    /// Parses a punctuated list.
    /// </summary>
    /// <typeparam name="T">The element type of the punctuated list.</typeparam>
    /// <param name="elementParser">The parser function that parses a single element.</param>
    /// <param name="punctType">The type of the punctuation token.</param>
    /// <param name="stopType">The type of the token that definitely ends this construct.</param>
    /// <param name="allowEmpty">True, if an empty list is allowed.</param>
    /// <returns>The parsed <see cref="PunctuatedList{T}"/>.</returns>
    private PunctuatedList<T> ParsePunctuatedListAllowTrailing<T>(
        Func<T> elementParser,
        TokenType punctType,
        TokenType stopType)
    {
        var elements = ValueArray.CreateBuilder<Punctuated<T>>();
        while (true)
        {
            // Stop token met, don't go further
            if (this.Peek().Type == stopType) break;
            // Parse an element
            var element = elementParser();
            // If the next token is not a punctuation, we are done
            if (this.Matches(punctType, out var punct))
            {
                // Punctuation, add with element
                elements.Add(new(element, punct));
            }
            else
            {
                // Not punctuation, we are done
                elements.Add(new(element, null));
                break;
            }
        }
        return new(elements.ToValue());
    }

    /// <summary>
    /// Parses an enclosed construct.
    /// </summary>
    /// <typeparam name="T">The type of the enclosed value.</typeparam>
    /// <param name="openType">The type of the token that starts this construct.</param>
    /// <param name="valueParser">The parser that parses the enclosed element.</param>
    /// <param name="closeType">The type of the token that closes the construct.</param>
    /// <returns>The parsed <see cref="Enclosed{T}"/>.</returns>
    private Enclosed<T> ParseEnclosed<T>(TokenType openType, Func<T> valueParser, TokenType closeType)
    {
        var openToken = this.Expect(openType);
        var value = valueParser();
        var closeToken = this.Expect(closeType);
        return new(openToken, value, closeToken);
    }

    // Token-level operators

    /// <summary>
    /// Expects a certain kind of token to be at the current position.
    /// If it is, the token is consumed.
    /// </summary>
    /// <param name="type">The expected token type.</param>
    /// <returns>The consumed <see cref="Token"/>.</returns>
    private Token Expect(TokenType type)
    {
        if (!this.Matches(type, out var token))
        {
            // We construct an empty token that signals that this is missing from the tree
            // The attached diagnostic message describes what is missing
            var location = new Location(0);
            var diag = Diagnostic.Create(SyntaxErrors.ExpectedToken, location, formatArgs: type);
            return Token.From(type, string.Empty, ValueArray.Create(diag));
        }
        return token;
    }

    /// <summary>
    /// Checks if the upcoming token has type <paramref name="type"/>.
    /// If it is, the token is consumed.
    /// </summary>
    /// <param name="type">The token type to match.</param>
    /// <param name="token">The matched token is written here.</param>
    /// <returns>True, if the upcoming token is of type <paramref name="type"/>.</returns>
    private bool Matches(TokenType type, [MaybeNullWhen(false)] out Token token)
    {
        if (this.Peek().Type == type)
        {
            token = this.Advance();
            return true;
        }
        else
        {
            token = null;
            return false;
        }
    }

    /// <summary>
    /// Peeks ahead in the token source.
    /// </summary>
    /// <param name="offset">The amount to peek ahead.</param>
    /// <returns>The <see cref="Token"/> that is <paramref name="offset"/> ahead.</returns>
    private Token Peek(int offset = 0) => this.tokenSource.Peek(offset);

    /// <summary>
    /// Advances the parser in the token source with one token.
    /// </summary>
    /// <returns>The consumed <see cref="Token"/>.</returns>
    private Token Advance()
    {
        var token = this.Peek();
        this.tokenSource.Advance();
        return token;
    }
}
