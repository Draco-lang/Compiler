using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Parses a sequence of <see cref="SyntaxToken"/>s into a <see cref="SyntaxNode"/>.
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
    /// Represents a parsed block.
    /// This is factored out because we parse blocks differently, and instantiating an AST node could be wasteful.
    /// </summary>
    /// <param name="OpenCurly">The open curly brace.</param>
    /// <param name="Statements">The list of statements.</param>
    /// <param name="Value">The evaluation value, if any.</param>
    /// <param name="CloseCurly">The close curly brace.</param>
    private readonly record struct ParsedBlock(
        SyntaxToken OpenCurly,
        SyntaxList<StatementSyntax> Statements,
        ExpressionSyntax? Value,
        SyntaxToken CloseCurly);

    /// <summary>
    /// Describes a single precedence level for expressions.
    /// </summary>
    /// <param name="Kind">The kind of the precedence level.</param>
    /// <param name="Operators">The operator tokens for this level.</param>
    /// <param name="CustomParser">The custom parser for the level, if any.</param>
    private readonly record struct PrecLevel(
        PrecLevelKind Kind,
        TokenType[] Operators,
        Func<Parser, Func<ExpressionSyntax>, ExpressionSyntax> CustomParser)
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
        public static PrecLevel Custom(Func<Parser, Func<ExpressionSyntax>, ExpressionSyntax> parserFunc) => new(
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
        TokenType.LiteralFloat,
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

    /// <summary>
    /// The <see cref="Diagnostic"/> messages attached to the nodes.
    /// </summary>
    internal ConditionalWeakTable<SyntaxNode, Diagnostic> Diagnostics { get; } = new();

    private readonly ITokenSource tokenSource;

    public Parser(ITokenSource tokenSource)
    {
        this.tokenSource = tokenSource;
    }

    /// <summary>
    /// Parses a <see cref="CompilationUnitSyntax"/> until the end of input.
    /// </summary>
    /// <returns>The parsed <see cref="CompilationUnitSyntax"/>.</returns>
    public CompilationUnitSyntax ParseCompilationUnit()
    {
        var decls = SyntaxList.CreateBuilder<DeclarationSyntax>();
        while (this.Peek() != TokenType.EndOfInput) decls.Add(this.ParseDeclaration());
        var end = this.Expect(TokenType.EndOfInput);
        return new(decls.ToSyntaxList(), end);
    }

    /// <summary>
    /// Parses a declaration.
    /// </summary>
    /// <returns>The parsed <see cref="DeclarationSyntax"/>.</returns>
    internal DeclarationSyntax ParseDeclaration()
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
            var location = GetLocation(input.Sum(i => i.Width));
            var diag = Diagnostic.Create(SyntaxErrors.UnexpectedInput, location, formatArgs: "declaration");
            var node = new UnexpectedDeclarationSyntax(input);
            this.Diagnostics.Add(node, diag);
            return node;
        }
        }
    }

    /// <summary>
    /// Parses a statement.
    /// </summary>
    /// <returns>The parsed <see cref="StatementSyntax"/>.</returns>
    internal StatementSyntax ParseStatement(bool allowDecl)
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
            return new DeclarationStatementSyntax(decl);
        }

        // Expressions that can appear without braces
        case TokenType.CurlyOpen:
        case TokenType.KeywordIf:
        case TokenType.KeywordWhile:
        {
            var expr = this.ParseControlFlowExpr(ControlFlowContext.Stmt);
            return new ExpressionStatementSyntax(expr, null);
        }

        // Assume expression
        default:
        {
            // TODO: This assumption might not be the best
            var expr = this.ParseExpression();
            var semicolon = this.Expect(TokenType.Semicolon);
            return new ExpressionStatementSyntax(expr, semicolon);
        }
        }
    }

    /// <summary>
    /// Parses a <see cref="VariableDeclarationSyntax"/>.
    /// </summary>
    /// <returns>The parsed <see cref="VariableDeclarationSyntax"/>.</returns>
    private VariableDeclarationSyntax ParseVariableDeclaration()
    {
        // NOTE: We will always call this function by checking the leading keyword
        var keyword = this.Advance();
        Debug.Assert(keyword.Type == TokenType.KeywordVal || keyword.Type == TokenType.KeywordVar);
        var identifier = this.Expect(TokenType.Identifier);
        // We don't necessarily have type specifier
        TypeSpecifierSyntax? type = null;
        if (this.Peek() == TokenType.Colon) type = this.ParseTypeSpecifier();
        // We don't necessarily have value assigned to the variable
        ValueSpecifierSyntax? assignment = null;
        if (this.Matches(TokenType.Assign, out var assign))
        {
            var value = this.ParseExpression();
            assignment = new(assign, value);
        }
        // Eat semicolon at the end of declaration
        var semicolon = this.Expect(TokenType.Semicolon);
        return new VariableDeclarationSyntax(keyword, identifier, type, assignment, semicolon);
    }

    /// <summary>
    /// Parsed a function declaration.
    /// </summary>
    /// <returns>The parsed <see cref="FunctionDeclarationSyntax"/>.</returns>
    private FunctionDeclarationSyntax ParseFuncDeclaration()
    {
        // Func keyword and name of the function
        var funcKeyword = this.Expect(TokenType.KeywordFunc);
        var name = this.Expect(TokenType.Identifier);

        // Parameters
        var openParen = this.Expect(TokenType.ParenOpen);
        var funcParameters = this.ParsePunctuatedListAllowTrailing(
            elementParser: this.ParseFuncParam,
            punctType: TokenType.Comma,
            stopType: TokenType.ParenClose);
        var closeParen = this.Expect(TokenType.ParenClose);

        // We don't necessarily have type specifier
        TypeSpecifierSyntax? returnType = null;
        if (this.Peek() == TokenType.Colon) returnType = this.ParseTypeSpecifier();

        var body = this.ParseFuncBody();

        return new FunctionDeclarationSyntax(funcKeyword, name, openParen, funcParameters, closeParen, returnType, body);
    }

    /// <summary>
    /// Parses a label declaration.
    /// </summary>
    /// <returns>The parsed <see cref="LabelDeclarationSyntax"/>.</returns>
    private LabelDeclarationSyntax ParseLabelDeclaration()
    {
        var labelName = this.Expect(TokenType.Identifier);
        var colon = this.Expect(TokenType.Colon);
        return new(labelName, colon);
    }

    /// <summary>
    /// Parses a function parameter.
    /// </summary>
    /// <returns>The parsed <see cref="ParameterSyntax"/>.</returns>
    private ParameterSyntax ParseFuncParam()
    {
        var name = this.Expect(TokenType.Identifier);
        var colon = this.Expect(TokenType.Colon);
        var type = this.ParseTypeExpr();
        return new(name, colon, type);
    }

    /// <summary>
    /// Parses a function body.
    /// </summary>
    /// <returns>The parsed <see cref="FunctionBodySyntax"/>.</returns>
    private FunctionBodySyntax ParseFuncBody()
    {
        if (this.Matches(TokenType.Assign, out var assign))
        {
            var expr = this.ParseExpression();
            var semicolon = this.Expect(TokenType.Semicolon);
            return new InlineFunctionBodySyntax(assign, expr, semicolon);
        }
        else if (this.Peek() == TokenType.CurlyOpen)
        {
            var block = this.ParseBlock(ControlFlowContext.Stmt);
            return new BlockFunctionBodySyntax(
                openBrace: block.OpenCurly,
                statements: block.Statements,
                closeBrace: block.CloseCurly);
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
            var location = GetLocation(input.Sum(i => i.Width));
            var diag = Diagnostic.Create(SyntaxErrors.UnexpectedInput, location, formatArgs: "function body");
            var node = new UnexpectedFunctionBodySyntax(input);
            this.Diagnostics.Add(node, diag);
            return node;
        }
    }

    /// <summary>
    /// Parses a type specifier.
    /// </summary>
    /// <returns>The parsed <see cref="TypeSpecifierSyntax"/>.</returns>
    private TypeSpecifierSyntax ParseTypeSpecifier()
    {
        var colon = this.Expect(TokenType.Colon);
        var type = this.ParseTypeExpr();
        return new(colon, type);
    }

    /// <summary>
    /// Parses a type expression.
    /// </summary>
    /// <returns>The parsed <see cref="TypeSyntax"/>.</returns>
    private TypeSyntax ParseTypeExpr()
    {
        if (this.Matches(TokenType.Identifier, out var typeName))
        {
            return new NameTypeSyntax(typeName);
        }
        else
        {
            var input = this.Synchronize(t => t switch
            {
                TokenType.Semicolon or TokenType.Comma
                or TokenType.ParenClose or TokenType.BracketClose
                or TokenType.CurlyClose or TokenType.InterpolationEnd
                or TokenType.Assign => false,
                var type when expressionStarters.Contains(type) => false,
                _ => true,
            });
            var location = GetLocation(input.Sum(i => i.Width));
            var diag = Diagnostic.Create(SyntaxErrors.UnexpectedInput, location, formatArgs: "type");
            var node = new UnexpectedTypeSyntax(input);
            this.Diagnostics.Add(node, diag);
            return node;
        }
    }

    /// <summary>
    /// Parses any kind of control-flow expression, like a block, if or while expression.
    /// </summary>
    /// <param name="ctx">The current context we are in.</param>
    /// <returns>The parsed <see cref="ExpressionSyntax"/>.</returns>
    private ExpressionSyntax ParseControlFlowExpr(ControlFlowContext ctx)
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
    /// <returns>The parsed <see cref="ExpressionSyntax"/>.</returns>
    private ExpressionSyntax ParseControlFlowBody(ControlFlowContext ctx)
    {
        if (ctx == ControlFlowContext.Expr)
        {
            // Only expressions, no semicolon needed
            return this.ParseExpression();
        }
        else
        {
            // Just a statement
            // Since this is a one-liner, we don't allow declarations as for example
            // if (x) var y = z;
            // makes no sense!
            var stmt = this.ParseStatement(allowDecl: false);
            return new StatementExpressionSyntax(stmt);
        }
    }

    /// <summary>
    /// Parses a code-block.
    /// </summary>
    /// <param name="ctx">The current context we are in.</param>
    /// <returns>The parsed <see cref="BlockExpressionSyntax"/>.</returns>
    private BlockExpressionSyntax ParseBlockExpr(ControlFlowContext ctx)
    {
        var parsed = this.ParseBlock(ctx);
        return new BlockExpressionSyntax(
            openBrace: parsed.OpenCurly,
            statements: parsed.Statements,
            value: parsed.Value,
            closeBrace: parsed.CloseCurly);
    }

    private ParsedBlock ParseBlock(ControlFlowContext ctx)
    {
        var openBrace = this.Expect(TokenType.CurlyOpen);
        var stmts = SyntaxList.CreateBuilder<StatementSyntax>();
        ExpressionSyntax? value = null;
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
                stmts.Add(new DeclarationStatementSyntax(decl));
                break;
            }

            case TokenType.CurlyOpen:
            case TokenType.KeywordIf:
            case TokenType.KeywordWhile:
            {
                var expr = this.ParseControlFlowExpr(ctx);
                if (ctx == ControlFlowContext.Expr && this.Peek() == TokenType.CurlyClose)
                {
                    // Treat as expression
                    value = expr;
                    goto end_of_block;
                }
                // Just a statement
                stmts.Add(new ExpressionStatementSyntax(expr, null));
                break;
            }

            default:
            {
                if (expressionStarters.Contains(this.Peek()))
                {
                    // Some expression
                    var expr = this.ParseExpression();
                    if (ctx == ControlFlowContext.Stmt || this.Peek() != TokenType.CurlyClose)
                    {
                        // Likely just a statement, can continue
                        var semicolon = this.Expect(TokenType.Semicolon);
                        stmts.Add(new ExpressionStatementSyntax(expr, semicolon));
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
                    var location = GetLocation(tokens.Sum(i => i.Width));
                    var diag = Diagnostic.Create(SyntaxErrors.UnexpectedInput, location, formatArgs: "statement");
                    var errNode = new UnexpectedStatementSyntax(tokens);
                    this.Diagnostics.Add(errNode, diag);
                    stmts.Add(errNode);
                }
                break;
            }
            }
        }
    end_of_block:
        var closeBrace = this.Expect(TokenType.CurlyClose);
        return new(openBrace, stmts.ToSyntaxList(), value, closeBrace);
    }

    /// <summary>
    /// Parses an if-expression.
    /// </summary>
    /// <param name="ctx">The current context we are in.</param>
    /// <returns>The parsed <see cref="IfExpressionSyntax"/>.</returns>
    private IfExpressionSyntax ParseIfExpr(ControlFlowContext ctx)
    {
        var ifKeyword = this.Expect(TokenType.KeywordIf);
        var openParen = this.Expect(TokenType.ParenOpen);
        var condition = this.ParseExpression();
        var closeParen = this.Expect(TokenType.ParenClose);
        var thenBody = this.ParseControlFlowBody(ctx);

        ElseClauseSyntax? elsePart = null;
        if (this.Matches(TokenType.KeywordElse, out var elseKeyword))
        {
            var elseBody = this.ParseControlFlowBody(ctx);
            elsePart = new(elseKeyword, elseBody);
        }

        return new(ifKeyword, openParen, condition, closeParen, thenBody, elsePart);
    }

    /// <summary>
    /// Parses a while-expression.
    /// </summary>
    /// <param name="ctx">The current context we are in.</param>
    /// <returns>The parsed <see cref="WhileExpressionSyntax"/>.</returns>
    private WhileExpressionSyntax ParseWhileExpr(ControlFlowContext ctx)
    {
        var whileKeyword = this.Expect(TokenType.KeywordWhile);
        var openParen = this.Expect(TokenType.ParenOpen);
        var condition = this.ParseExpression();
        var closeParen = this.Expect(TokenType.ParenClose);
        var body = this.ParseControlFlowBody(ctx);
        return new(whileKeyword, openParen, condition, closeParen, body);
    }

    /// <summary>
    /// Parses an expression.
    /// </summary>
    /// <returns>The parsed <see cref="ExpressionSyntax"/>.</returns>
    internal ExpressionSyntax ParseExpression()
    {
        // The function that is driven by the precedence table
        ExpressionSyntax ParsePrecedenceLevel(int level)
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
                    return new UnaryExpressionSyntax(op, subexpr);
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
                    result = new BinaryExpressionSyntax(result, op, right);
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
                    result = new BinaryExpressionSyntax(result, op, right);
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

    private ExpressionSyntax ParsePseudoStmtLevelExpr(Func<ExpressionSyntax> elementParser)
    {
        switch (this.Peek())
        {
        case TokenType.KeywordReturn:
        {
            var returnKeyword = this.Advance();
            ExpressionSyntax? value = null;
            if (expressionStarters.Contains(this.Peek())) value = this.ParseExpression();
            return new ReturnExpressionSyntax(returnKeyword, value);
        }

        case TokenType.KeywordGoto:
        {
            var gotoKeyword = this.Advance();
            var labelName = this.Expect(TokenType.Identifier);
            return new GotoExpressionSyntax(gotoKeyword, new NameLabelSyntax(labelName));
        }

        default:
            return elementParser();
        }
    }

    private ExpressionSyntax ParseRelationalLevelExpr(Func<ExpressionSyntax> elementParser)
    {
        var left = elementParser();
        var comparisons = SyntaxList.CreateBuilder<ComparisonElementSyntax>();
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
            : new RelationalExpressionSyntax(left, comparisons.ToSyntaxList());
    }

    private ExpressionSyntax ParseCallLevelExpr(Func<ExpressionSyntax> elementParser)
    {
        var result = elementParser();
        while (true)
        {
            var peek = this.Peek();
            if (peek == TokenType.ParenOpen)
            {
                var openParen = this.Expect(TokenType.ParenOpen);
                var args = this.ParsePunctuatedListAllowTrailing(
                    elementParser: this.ParseExpression,
                    punctType: TokenType.Comma,
                    stopType: TokenType.ParenClose);
                var closeParen = this.Expect(TokenType.ParenClose);
                result = new CallExpressionSyntax(result, openParen, args, closeParen);
            }
            else if (peek == TokenType.BracketOpen)
            {
                var openParen = this.Expect(TokenType.ParenOpen);
                var args = this.ParsePunctuatedListAllowTrailing(
                    elementParser: this.ParseExpression,
                    punctType: TokenType.Comma,
                    stopType: TokenType.BracketClose);
                var closeParen = this.Expect(TokenType.ParenClose);
                result = new IndexExpressionSyntax(result, openParen, args, closeParen);
            }
            else if (this.Matches(TokenType.Dot, out var dot))
            {
                var name = this.Expect(TokenType.Identifier);
                result = new MemberAccessExpressionSyntax(result, dot, name);
            }
            else
            {
                break;
            }
        }
        return result;
    }

    private ExpressionSyntax ParseAtomExpr()
    {
        switch (this.Peek())
        {
        case TokenType.LiteralInteger:
        case TokenType.LiteralFloat:
        case TokenType.LiteralCharacter:
        {
            var value = this.Advance();
            return new LiteralExpressionSyntax(value);
        }
        case TokenType.KeywordTrue:
        case TokenType.KeywordFalse:
        {
            var value = this.Advance();
            return new LiteralExpressionSyntax(value);
        }
        case TokenType.LineStringStart:
            return this.ParseLineString();
        case TokenType.MultiLineStringStart:
            return this.ParseMultiLineString();
        case TokenType.Identifier:
        {
            var name = this.Advance();
            return new NameExpressionSyntax(name);
        }
        case TokenType.ParenOpen:
        {
            var openParen = this.Expect(TokenType.ParenOpen);
            var expr = this.ParseExpression();
            var closeParen = this.Expect(TokenType.ParenClose);
            return new GroupingExpressionSyntax(openParen, expr, closeParen);
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
                or TokenType.ParenClose or TokenType.BracketClose
                or TokenType.CurlyClose or TokenType.InterpolationEnd => false,
                var type when expressionStarters.Contains(type) => false,
                _ => true,
            });
            var location = GetLocation(input.Sum(i => i.Width));
            var diag = Diagnostic.Create(SyntaxErrors.UnexpectedInput, location, formatArgs: "expression");
            var node = new UnexpectedExpressionSyntax(input);
            this.Diagnostics.Add(node, diag);
            return node;
        }
        }
    }

    /// <summary>
    /// Parses a line string expression.
    /// </summary>
    /// <returns>The parsed <see cref="StringExpressionSyntax"/>.</returns>
    private StringExpressionSyntax ParseLineString()
    {
        var openQuote = this.Expect(TokenType.LineStringStart);
        var content = SyntaxList.CreateBuilder<StringPartSyntax>();
        while (true)
        {
            var peek = this.Peek();
            if (peek == TokenType.StringContent)
            {
                var part = this.Advance();
                content.Add(new TextStringPartSyntax(part));
            }
            else if (peek == TokenType.InterpolationStart)
            {
                var start = this.Advance();
                var expr = this.ParseExpression();
                var end = this.Expect(TokenType.InterpolationEnd);
                content.Add(new InterpolationStringPartSyntax(start, expr, end));
            }
            else
            {
                // We need a close quote for line strings then
                break;
            }
        }
        var closeQuote = this.Expect(TokenType.LineStringEnd);
        return new(openQuote, content.ToSyntaxList(), closeQuote);
    }

    /// <summary>
    /// Parses a multi-line string expression.
    /// </summary>
    /// <returns>The parsed <see cref="StringExpressionSyntax"/>.</returns>
    private StringExpressionSyntax ParseMultiLineString()
    {
        var openQuote = this.Expect(TokenType.MultiLineStringStart);
        var content = SyntaxList.CreateBuilder<StringPartSyntax>();
        // We check if there's a newline
        if (!openQuote.TrailingTrivia.Any(t => t.Type == TriviaType.Newline))
        {
            // Possible stray tokens inline
            var strayTokens = this.Synchronize(t => t switch
            {
                TokenType.MultiLineStringEnd or TokenType.StringNewline => false,
                _ => true,
            });
            var location = GetLocation(strayTokens.Sum(t => t.Width));
            var diag = Diagnostic.Create(
                SyntaxErrors.ExtraTokensInlineWithOpenQuotesOfMultiLineString,
                location);
            var unexpected = new UnexpectedStringPartSyntax(strayTokens);
            this.Diagnostics.Add(unexpected, diag);
            content.Add(unexpected);
        }
        while (true)
        {
            var peek = this.Peek();
            if (peek == TokenType.StringContent || peek == TokenType.StringNewline)
            {
                var part = this.Advance();
                content.Add(new TextStringPartSyntax(part));
            }
            else if (peek == TokenType.InterpolationStart)
            {
                var start = this.Advance();
                var expr = this.ParseExpression();
                var end = this.Expect(TokenType.InterpolationEnd);
                content.Add(new InterpolationStringPartSyntax(start, expr, end));
            }
            else
            {
                // We need a close quote for line strings then
                break;
            }
        }
        var closeQuote = this.Expect(TokenType.MultiLineStringEnd);
        // We need to check if the close quote is on a newline
        // There are 2 cases:
        //  - the leading trivia of the closing quotes contains a newline
        //  - the string is empty and the opening quotes trailing trivia contains a newline
        var isClosingQuoteOnNewline =
               closeQuote.LeadingTrivia.Count > 0
            || (content.Count == 0 && openQuote.TrailingTrivia.Any(t => t.Type == TriviaType.Newline));
        if (isClosingQuoteOnNewline)
        {
            Debug.Assert(closeQuote.LeadingTrivia.Count <= 2);
            Debug.Assert(openQuote.TrailingTrivia.Any(t => t.Type == TriviaType.Newline)
                      || closeQuote.LeadingTrivia.Any(t => t.Type == TriviaType.Newline));
            if (closeQuote.LeadingTrivia.Count == 2)
            {
                // The first trivia was newline, the second must be spaces
                Debug.Assert(closeQuote.LeadingTrivia[1].Type == TriviaType.Whitespace);
                // We take the whitespace text and check if every line in the string obeys that as a prefix
                var prefix = closeQuote.LeadingTrivia[1].Text;
                var nextIsNewline = true;
                foreach (var part in content)
                {
                    if (part is TextStringPartSyntax textPart)
                    {
                        if (textPart.Content.Type == TokenType.StringNewline)
                        {
                            // Also a newline, don't care, even an empty line is fine
                            nextIsNewline = true;
                            continue;
                        }
                        // Actual text content
                        if (nextIsNewline && !textPart.Content.Text.StartsWith(prefix))
                        {
                            // We are in a newline and the prefixes don't match, that's an error
                            var whitespaceLength = textPart.Content.Text.TakeWhile(char.IsWhiteSpace).Count();
                            var location = GetLocation(whitespaceLength);
                            var diag = Diagnostic.Create(
                                SyntaxErrors.InsufficientIndentationInMultiLinString,
                                location);
                            this.Diagnostics.Add(textPart, diag);
                        }
                        else
                        {
                            // Indentation was ok
                        }
                        nextIsNewline = false;
                    }
                    else
                    {
                        // Interpolation, don't care
                        nextIsNewline = false;
                    }
                }
            }
        }
        else
        {
            // Error, the closing quotes are not on a newline
            var location = GetLocation(closeQuote.Width);
            var diag = Diagnostic.Create(
                SyntaxErrors.ClosingQuotesOfMultiLineStringNotOnNewLine,
                location);
            this.Diagnostics.Add(closeQuote, diag);
        }
        return new(openQuote, content.ToSyntaxList(), closeQuote);
    }

    // General utilities

    /// <summary>
    /// Parses a <see cref="SeparatedSyntaxList{TNode}"/>.
    /// </summary>
    /// <typeparam name="TNode">The element type of the list.</typeparam>
    /// <param name="elementParser">The parser function that parses a single element.</param>
    /// <param name="punctType">The type of the punctuation token.</param>
    /// <param name="stopType">The type of the token that definitely ends this construct.</param>
    /// <returns>The parsed <see cref="SeparatedSyntaxList{TNode}"/>.</returns>
    private SeparatedSyntaxList<TNode> ParsePunctuatedListAllowTrailing<TNode>(
        Func<TNode> elementParser,
        TokenType punctType,
        TokenType stopType)
        where TNode : SyntaxNode
    {
        var elements = SeparatedSyntaxList.CreateBuilder<TNode>();
        while (true)
        {
            // Stop token met, don't go further
            if (this.Peek() == stopType) break;
            // Parse an element
            var element = elementParser();
            elements.Add(element);
            // If the next token is not a punctuation, we are done
            if (!this.Matches(punctType, out var punct)) break;
            // We had a punctuation, we can continue
            elements.Add(punct);
        }
        return elements.ToSeparatedSyntaxList();
    }

    // Token-level operators

    /// <summary>
    /// Performs synchronization, meaning it consumes <see cref="SyntaxToken"/>s from the input
    /// while a given condition is met.
    /// </summary>
    /// <param name="keepGoing">The predicate that dictates if the consumption should keep going.</param>
    /// <returns>The consumed list of <see cref="SyntaxToken"/>s as <see cref="SyntaxNode"/>s.</returns>
    private SyntaxList<SyntaxNode> Synchronize(Func<TokenType, bool> keepGoing)
    {
        // NOTE: A possible improvement could be to track opening and closing token pairs optionally
        var input = SyntaxList.CreateBuilder<SyntaxNode>();
        while (true)
        {
            var peek = this.Peek();
            if (peek == TokenType.EndOfInput) break;
            if (!keepGoing(peek)) break;
            input.Add(this.Advance());
        }
        return input.ToSyntaxList();
    }

    /// <summary>
    /// Expects a certain kind of token to be at the current position.
    /// If it is, the token is consumed.
    /// </summary>
    /// <param name="type">The expected token type.</param>
    /// <returns>The consumed <see cref="SyntaxToken"/>.</returns>
    private SyntaxToken Expect(TokenType type)
    {
        if (!this.Matches(type, out var token))
        {
            // We construct an empty token that signals that this is missing from the tree
            // The attached diagnostic message describes what is missing
            var location = GetLocation(0);
            var tokenTypeName = type.GetUserFriendlyName();
            var diag = Diagnostic.Create(SyntaxErrors.ExpectedToken, location, formatArgs: tokenTypeName);
            token = SyntaxToken.From(type, string.Empty);
            this.Diagnostics.Add(token, diag);
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
    private bool Matches(TokenType type, [MaybeNullWhen(false)] out SyntaxToken token)
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
    /// <returns>The <see cref="TokenType"/> of the <see cref="SyntaxToken"/> that is <paramref name="offset"/>
    /// ahead.</returns>
    private TokenType Peek(int offset = 0) =>
        this.tokenSource.Peek(offset).Type;

    /// <summary>
    /// Advances the parser in the token source with one token.
    /// </summary>
    /// <returns>The consumed <see cref="SyntaxToken"/>.</returns>
    private SyntaxToken Advance()
    {
        var token = this.tokenSource.Peek();
        this.tokenSource.Advance();
        return token;
    }

    // Location utility

    private static Location GetLocation(int width) => new Location.RelativeToTree(Range: new(Offset: 0, Width: width));
}
