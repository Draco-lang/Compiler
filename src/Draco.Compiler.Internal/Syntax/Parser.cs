using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Utilities;
using static Draco.Compiler.Internal.Syntax.ParseTree;

namespace Draco.Compiler.Internal.Syntax;

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
        var decls = ImmutableArray.CreateBuilder<Decl>();
        while (this.Peek() != TokenType.EndOfInput) decls.Add(this.ParseDeclaration());
        var end = this.Expect(TokenType.EndOfInput);
        return new(decls.ToImmutable(), end);
    }

    /// <summary>
    /// Parses a declaration.
    /// </summary>
    /// <returns>The parsed <see cref="Decl"/>.</returns>
    private Decl ParseDeclaration()
    {
        switch (this.Peek())
        {
        case TokenType.KeywordFunc:
            return this.ParseFuncDeclaration();

        case TokenType.KeywordVar:
        case TokenType.KeywordVal:
            return this.ParseVariableDeclaration();

        case TokenType.Identifier when this.Peek(1) == TokenType.Colon:
            return this.ParseLabelDeclaration();

        default:
        {
            var input = this.Synchronize(t => t switch
            {
                TokenType.KeywordFunc or TokenType.KeywordVar or TokenType.KeywordVal => false,
                TokenType.Identifier when this.Peek(1) == TokenType.Colon => false,
                _ => true,
            });
            var location = new Location(0);
            var diag = Diagnostic.Create(SyntaxErrors.UnexpectedInput, location, formatArgs: "declaration");
            return new Decl.Unexpected(input, ImmutableArray.Create(diag));
        }
        }
    }

    /// <summary>
    /// Parses a statement.
    /// </summary>
    /// <returns>The parsed <see cref="Stmt"/>.</returns>
    private Stmt ParseStatement(bool allowDecl)
    {
        switch (this.Peek())
        {
        // Declarations
        case TokenType.KeywordFunc when allowDecl:
        case TokenType.KeywordVar when allowDecl:
        case TokenType.KeywordVal when allowDecl:
        case TokenType.Identifier when allowDecl && this.Peek(1) == TokenType.Colon:
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
        // NOTE: We will always call this function by checking the leading keyword
        var keyword = this.Advance();
        Debug.Assert(keyword.Type == TokenType.KeywordVal || keyword.Type == TokenType.KeywordVar);
        var identifier = this.Expect(TokenType.Identifier);
        // We don't necessarily have type specifier
        TypeSpecifier? type = null;
        if (this.Peek() == TokenType.Colon) type = this.ParseTypeSpecifier();
        // We don't necessarily have value assigned to the variable
        ValueInitializer? assignment = null;
        if (this.Matches(TokenType.Assign, out var assign))
        {
            var value = this.ParseExpr();
            assignment = new(assign, value);
        }
        // Eat semicolon at the end of declaration
        var semicolon = this.Expect(TokenType.Semicolon);
        return new Decl.Variable(keyword, identifier, type, assignment, semicolon);
    }

    /// <summary>
    /// Parsed a function declaration.
    /// </summary>
    /// <returns>The parsed <see cref="Decl.Func"/>.</returns>
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
        if (this.Peek() == TokenType.Colon) returnType = this.ParseTypeSpecifier();

        var body = this.ParseFuncBody();

        return new Decl.Func(funcKeyword, name, funcParameters, returnType, body);
    }

    /// <summary>
    /// Parses a label declaration.
    /// </summary>
    /// <returns>The parsed <see cref="Decl.Label"/>.</returns>
    private Decl.Label ParseLabelDeclaration()
    {
        var labelName = this.Expect(TokenType.Identifier);
        var colon = this.Expect(TokenType.Colon);
        return new(labelName, colon);
    }

    /// <summary>
    /// Parses a function parameter.
    /// </summary>
    /// <returns>The parsed <see cref="FuncParam"/>.</returns>
    private FuncParam ParseFuncParam()
    {
        var name = this.Expect(TokenType.Identifier);
        var typeSpec = this.ParseTypeSpecifier();
        return new(name, typeSpec);
    }

    /// <summary>
    /// Parses a function body.
    /// </summary>
    /// <returns>The parsed <see cref="FuncBody"/>.</returns>
    private FuncBody ParseFuncBody()
    {
        if (this.Matches(TokenType.Assign, out var assign))
        {
            var expr = this.ParseExpr();
            var semicolon = this.Expect(TokenType.Semicolon);
            return new FuncBody.InlineBody(assign, expr, semicolon);
        }
        else if (this.Peek() == TokenType.CurlyOpen)
        {
            var block = this.ParseBlockExpr(ControlFlowContext.Stmt);
            return new FuncBody.BlockBody(block);
        }
        else
        {
            // NOTE: I'm not sure what's the best strategy here
            // Maybe if we get to a '=' or '{' we could actually try to re-parse and prepend with the bogus input
            var input = this.Synchronize(t => t switch
            {
                TokenType.Semicolon or TokenType.CurlyClose
                or TokenType.KeywordFunc or TokenType.KeywordVar or TokenType.KeywordVal => false,
                _ => true,
            });
            var location = new Location(0);
            var diag = Diagnostic.Create(SyntaxErrors.UnexpectedInput, location, formatArgs: "function body");
            return new FuncBody.Unexpected(input, ImmutableArray.Create(diag));
        }
    }

    /// <summary>
    /// Parses a type specifier.
    /// </summary>
    /// <returns>The parsed <see cref="TypeSpecifier"/>.</returns>
    private TypeSpecifier ParseTypeSpecifier()
    {
        var colon = this.Expect(TokenType.Colon);
        var type = this.ParseTypeExpr();
        return new(colon, type);
    }

    /// <summary>
    /// Parses a type expression.
    /// </summary>
    /// <returns>The parsed <see cref="TypeExpr"/>.</returns>
    private TypeExpr ParseTypeExpr()
    {
        // For now we only allow identifiers
        var typeName = this.Expect(TokenType.Identifier);
        return new TypeExpr.Name(typeName);
    }

    /// <summary>
    /// Parses any kind of control-flow expression, like a block, if or while expression.
    /// </summary>
    /// <param name="ctx">The current context we are in.</param>
    /// <returns>The parsed <see cref="Expr"/>.</returns>
    private Expr ParseControlFlowExpr(ControlFlowContext ctx)
    {
        var peekType = this.Peek();
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

    /// <summary>
    /// Parses the body of a control-flow expression.
    /// </summary>
    /// <param name="ctx">The current context we are in.</param>
    /// <returns>The parsed <see cref="Expr"/>.</returns>
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

    /// <summary>
    /// Parses a code-block.
    /// </summary>
    /// <param name="ctx">The current context we are in.</param>
    /// <returns>The parsed <see cref="Expr.Block"/>.</returns>
    private Expr.Block ParseBlockExpr(ControlFlowContext ctx)
    {
        var enclosed = this.ParseEnclosed(
            openType: TokenType.CurlyOpen,
            valueParser: () =>
            {
                var stmts = ImmutableArray.CreateBuilder<Stmt>();
                Expr? value = null;
                while (true)
                {
                    switch (this.Peek())
                    {
                    case TokenType.EndOfInput:
                    case TokenType.CurlyClose:
                        // On a close curly or out of input, we can immediately exit
                        goto end_of_block;

                    case TokenType.KeywordFunc:
                    case TokenType.KeywordVar:
                    case TokenType.KeywordVal:
                    case TokenType.Identifier when this.Peek(1) == TokenType.Colon:
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
                        if (this.Peek() == TokenType.CurlyClose)
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
                        if (expressionStarters.Contains(this.Peek()))
                        {
                            // Some expression
                            var expr = this.ParseExpr();
                            if (this.Peek() != TokenType.CurlyClose)
                            {
                                // Likely just a statement, can continue
                                var semicolon = this.Expect(TokenType.Semicolon);
                                stmts.Add(new Stmt.Expr(expr, semicolon));
                            }
                            else
                            {
                                // This is the value of the block
                                value = expr;
                                goto end_of_block;
                            }
                        }
                        else
                        {
                            // Error, synchronize
                            var tokens = this.Synchronize(type => type switch
                            {
                                TokenType.CurlyClose
                                or TokenType.KeywordFunc or TokenType.KeywordVar or TokenType.KeywordVal
                                or TokenType.Identifier when this.Peek(1) == TokenType.Colon => false,
                                var tt when expressionStarters.Contains(tt) => false,
                                _ => true,
                            });
                            var location = new Location(0);
                            var diag = Diagnostic.Create(SyntaxErrors.UnexpectedInput, location, formatArgs: "statement");
                            stmts.Add(new Stmt.Unexpected(tokens, ImmutableArray.Create(diag)));
                        }
                        break;
                    }
                    }
                }
            end_of_block:
                return new BlockContents(stmts.ToImmutable(), value);
            },
            closeType: TokenType.CurlyClose);
        return new(enclosed);
    }

    /// <summary>
    /// Parses an if-expression.
    /// </summary>
    /// <param name="ctx">The current context we are in.</param>
    /// <returns>The parsed <see cref="Expr.If"/>.</returns>
    private Expr.If ParseIfExpr(ControlFlowContext ctx)
    {
        var ifKeyword = this.Expect(TokenType.KeywordIf);
        var condition = this.ParseEnclosed(
            openType: TokenType.ParenOpen,
            valueParser: this.ParseExpr,
            closeType: TokenType.ParenClose);
        var thenBody = this.ParseControlFlowBody(ctx);

        ElseClause? elsePart = null;
        if (this.Matches(TokenType.KeywordElse, out var elseKeyword))
        {
            var elseBody = this.ParseControlFlowBody(ctx);
            elsePart = new(elseKeyword, elseBody);
        }

        return new(ifKeyword, condition, thenBody, elsePart);
    }

    /// <summary>
    /// Parses a while-expression.
    /// </summary>
    /// <param name="ctx">The current context we are in.</param>
    /// <returns>The parsed <see cref="Expr.While"/>.</returns>
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

    /// <summary>
    /// Parses an expression.
    /// </summary>
    /// <returns>The parsed <see cref="Expr"/>.</returns>
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
                var opType = this.Peek();
                if (desc.Operators.Contains(opType))
                {
                    // There is such operator on this level
                    var op = this.Advance();
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
                    var opType = this.Peek();
                    if (!desc.Operators.Contains(opType)) break;
                    var op = this.Advance();
                    var right = ParsePrecedenceLevel(level - 1);
                    result = new Expr.Binary(result, op, right);
                }
                return result;
            }
            case PrecLevelKind.BinaryRight:
            {
                // Right-associativity is simply right-recursion
                var result = ParsePrecedenceLevel(level - 1);
                var opType = this.Peek();
                if (desc.Operators.Contains(this.Peek()))
                {
                    var op = this.Advance();
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

    // Plumbing code for precedence parsing

    private Expr ParsePseudoStmtLevelExpr(Func<Expr> elementParser)
    {
        switch (this.Peek())
        {
        case TokenType.KeywordReturn:
        {
            var returnKeyword = this.Advance();
            Expr? value = null;
            if (expressionStarters.Contains(this.Peek())) value = this.ParseExpr();
            return new Expr.Return(returnKeyword, value);
        }

        case TokenType.KeywordGoto:
        {
            var gotoKeyword = this.Advance();
            var labelName = this.Expect(TokenType.Identifier);
            return new Expr.Goto(gotoKeyword, new Expr.Name(labelName));
        }

        default:
            return elementParser();
        }
    }

    private Expr ParseRelationalLevelExpr(Func<Expr> elementParser)
    {
        var left = elementParser();
        var comparisons = ImmutableArray.CreateBuilder<ComparisonElement>();
        while (true)
        {
            var opType = this.Peek();
            if (!relationalOps.Contains(opType)) break;
            var op = this.Advance();
            var right = elementParser();
            comparisons.Add(new(op, right));
        }
        return comparisons.Count == 0
            ? left
            : new Expr.Relational(left, comparisons.ToImmutable());
    }

    private Expr ParseCallLevelExpr(Func<Expr> elementParser)
    {
        var result = elementParser();
        while (true)
        {
            var peek = this.Peek();
            if (peek == TokenType.ParenOpen)
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
            else if (peek == TokenType.BracketOpen)
            {
                var args = this.ParseEnclosed(
                    openType: TokenType.BracketOpen,
                    valueParser: () => this.ParsePunctuatedListAllowTrailing(
                        elementParser: this.ParseExpr,
                        punctType: TokenType.Comma,
                        stopType: TokenType.BracketClose),
                    closeType: TokenType.BracketClose);
                result = new Expr.Index(result, args);
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
        switch (this.Peek())
        {
        case TokenType.LiteralInteger:
        case TokenType.LiteralCharacter:
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
        case TokenType.LineStringStart:
            return this.ParseLineString();
        case TokenType.MultiLineStringStart:
            return this.ParseMultiLineString();
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
            var input = this.Synchronize(t => t switch
            {
                TokenType.Semicolon or TokenType.Comma
                or TokenType.ParenClose or TokenType.BracketClose or TokenType.CurlyClose => false,
                var type when expressionStarters.Contains(type) => false,
                _ => true,
            });
            var location = new Location(0);
            var diag = Diagnostic.Create(SyntaxErrors.UnexpectedInput, location, formatArgs: "expression");
            return new Expr.Unexpected(input, ImmutableArray.Create(diag));
        }
        }
    }

    /// <summary>
    /// Parses a line string expression.
    /// </summary>
    /// <returns>The parsed <see cref="Expr.String"/></returns>
    private Expr.String ParseLineString()
    {
        var openQuote = this.Expect(TokenType.LineStringStart);
        var content = ImmutableArray.CreateBuilder<StringPart>();
        while (true)
        {
            var peek = this.Peek();
            if (peek == TokenType.StringContent)
            {
                var part = this.Advance();
                content.Add(new StringPart.Content(part, 0, ImmutableArray<Diagnostic>.Empty));
            }
            else if (peek == TokenType.InterpolationStart)
            {
                var start = this.Advance();
                var expr = this.ParseExpr();
                var end = this.Expect(TokenType.InterpolationEnd);
                content.Add(new StringPart.Interpolation(start, expr, end));
            }
            else
            {
                // We need a close quote for line strings then
                break;
            }
        }
        var closeQuote = this.Expect(TokenType.LineStringEnd);
        return new(openQuote, content.ToImmutable(), closeQuote);
    }

    /// <summary>
    /// Parses a multi-line string expression.
    /// </summary>
    /// <returns>The parsed <see cref="Expr.String"/></returns>
    private Expr.String ParseMultiLineString()
    {
        var openQuote = this.Expect(TokenType.MultiLineStringStart);
        var content = ImmutableArray.CreateBuilder<StringPart>();
        while (true)
        {
            var peek = this.Peek();
            if (peek == TokenType.StringContent || peek == TokenType.StringNewline)
            {
                var part = this.Advance();
                content.Add(new StringPart.Content(part, 0, ImmutableArray<Diagnostic>.Empty));
            }
            else if (peek == TokenType.InterpolationStart)
            {
                var start = this.Advance();
                var expr = this.ParseExpr();
                var end = this.Expect(TokenType.InterpolationEnd);
                content.Add(new StringPart.Interpolation(start, expr, end));
            }
            else
            {
                // We need a close quote for line strings then
                break;
            }
        }
        var closeQuote = this.Expect(TokenType.MultiLineStringEnd);
        // We need to check if the close quote is on a newline
        if (closeQuote.LeadingTrivia.Length > 0)
        {
            Debug.Assert(closeQuote.LeadingTrivia.Length <= 2);
            Debug.Assert(closeQuote.LeadingTrivia[0].Type == TokenType.Newline);
            if (closeQuote.LeadingTrivia.Length == 2)
            {
                // The first trivia was newline, the second must be spaces
                Debug.Assert(closeQuote.LeadingTrivia[1].Type == TokenType.Whitespace);
                // For simplicity we rebuild the contents to be able to append diagnostics
                var newContent = ImmutableArray.CreateBuilder<StringPart>();
                // We take the whitespace text and check if every line in the string obeys that as a prefix
                var prefix = closeQuote.LeadingTrivia[1].Text;
                var nextIsNewline = true;
                foreach (var part in content)
                {
                    if (part is StringPart.Content contentPart)
                    {
                        if (contentPart.Value.Type == TokenType.StringNewline)
                        {
                            // Also a newline, don't care, even an empty line is fine
                            newContent.Add(part);
                            nextIsNewline = true;
                            continue;
                        }
                        // Actual text content
                        if (nextIsNewline && !contentPart.Value.Text.StartsWith(prefix))
                        {
                            // We are in a newline and the prefixes don't match, that's an error
                            var location = new Location(0);
                            var diag = Diagnostic.Create(
                                SyntaxErrors.InsufficientIndentationInMultiLinString,
                                location);
                            var allDiags = contentPart.Diagnostics.Append(diag).ToImmutableArray();
                            newContent.Add(new StringPart.Content(contentPart.Value, 0, allDiags));
                        }
                        else
                        {
                            // Indentation was ok, reinstantiate to add cutoff
                            newContent.Add(new StringPart.Content(contentPart.Value, prefix.Length, contentPart.Diagnostics));
                        }
                        nextIsNewline = false;
                    }
                    else
                    {
                        // Interpolation, don't care
                        newContent.Add(part);
                        nextIsNewline = false;
                    }
                }
                content = newContent;
            }
        }
        else
        {
            // Error, the closing quotes are not on a newline
            var location = new Location(0);
            var diag = Diagnostic.Create(
                SyntaxErrors.ClosingQuotesOfMultiLineStringNotOnNewLine,
                location);
            closeQuote = closeQuote.AddDiagnostic(diag);
        }
        return new(openQuote, content.ToImmutable(), closeQuote);
    }

    // General utilities

    /// <summary>
    /// Parses a punctuated list.
    /// </summary>
    /// <typeparam name="T">The element type of the punctuated list.</typeparam>
    /// <param name="elementParser">The parser function that parses a single element.</param>
    /// <param name="punctType">The type of the punctuation token.</param>
    /// <param name="stopType">The type of the token that definitely ends this construct.</param>
    /// <returns>The parsed <see cref="PunctuatedList{T}"/>.</returns>
    private PunctuatedList<T> ParsePunctuatedListAllowTrailing<T>(
        Func<T> elementParser,
        TokenType punctType,
        TokenType stopType)
    {
        var elements = ImmutableArray.CreateBuilder<Punctuated<T>>();
        while (true)
        {
            // Stop token met, don't go further
            if (this.Peek() == stopType) break;
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
        return new(elements.ToImmutable());
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
    /// Performs synchronization, meaning it consumes <see cref="Token"/>s from the input
    /// while a given condition is met.
    /// </summary>
    /// <param name="keepGoing">The predicate that dictates if the consumption should keep going.</param>
    /// <returns>The consumed list of <see cref="Token"/>s.</returns>
    private ImmutableArray<Token> Synchronize(Func<TokenType, bool> keepGoing)
    {
        // NOTE: A possible improvement could be to track opening and closing token pairs optionally
        var input = ImmutableArray.CreateBuilder<Token>();
        while (true)
        {
            var peek = this.Peek();
            if (peek == TokenType.EndOfInput) break;
            if (!keepGoing(peek)) break;
            input.Add(this.Advance());
        }
        return input.ToImmutable();
    }

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
            return Token.From(type, string.Empty, ImmutableArray.Create(diag));
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
        if (this.Peek() == type)
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
    /// Peeks ahead the type of a token in the token source.
    /// </summary>
    /// <param name="offset">The amount to peek ahead.</param>
    /// <param name="default">The default <see cref="TokenType"/> to return in case of end of input.</param>
    /// <returns>The <see cref="TokenType"/> of the <see cref="Token"/> that is <paramref name="offset"/>
    /// ahead.</returns>
    private TokenType Peek(int offset = 0, TokenType @default = TokenType.EndOfInput) =>
        this.tokenSource.Peek(offset).Type;

    /// <summary>
    /// Advances the parser in the token source with one token.
    /// </summary>
    /// <returns>The consumed <see cref="Token"/>.</returns>
    private Token Advance()
    {
        var token = this.tokenSource.Peek();
        this.tokenSource.Advance();
        return token;
    }
}
