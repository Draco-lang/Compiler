using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// The different parser modes.
/// </summary>
internal enum ParserMode
{
    /// <summary>
    /// The default parsing mode.
    /// </summary>
    None,

    /// <summary>
    /// A mode that bails out as early as possible for expression parsing when a newline is encountered.
    /// </summary>
    Repl,
}

/// <summary>
/// Parses a sequence of <see cref="SyntaxToken"/>s into a <see cref="SyntaxNode"/>.
/// </summary>
internal sealed class Parser(
    ITokenSource tokenSource,
    SyntaxDiagnosticTable diagnostics,
    ParserMode parserMode = ParserMode.None)
{
    /// <summary>
    /// The different declaration contexts.
    /// </summary>
    private enum DeclarationContext
    {
        /// <summary>
        /// Global, like in a compilation unit, module, class, ...
        /// </summary>
        Global,

        /// <summary>
        /// Local to a function body/expression/code-block.
        /// </summary>
        Local,
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
    /// The result of trying to disambiguate '<'.
    /// </summary>
    private enum LessThanDisambiguation
    {
        /// <summary>
        /// Must be an operator for comparison.
        /// </summary>
        Operator,

        /// <summary>
        /// Must be a generic parameter list.
        /// </summary>
        Generics,
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
    /// Represents a parsed generic argument list.
    /// This is factored out as it's reused in expression and type contexts with error handling, and separate
    /// AST nodes would be wasteful since these are always inlined.
    /// </summary>
    /// <param name="OpenBracked">The open bracket.</param>
    /// <param name="Arguments">The list of generic arguments.</param>
    /// <param name="CloseBracket">The close bracket.</param>
    private readonly record struct ParsedGenericArgumentList(
        SyntaxToken OpenBracked,
        SeparatedSyntaxList<TypeSyntax> Arguments,
        SyntaxToken CloseBracket);

    /// <summary>
    /// A delegate for an <see cref="ExpressionSyntax"/> parser.
    /// </summary>
    /// <param name="level">The level in the precedence table.</param>
    /// <returns>The parsed <see cref="ExpressionSyntax"/>.</returns>
    private delegate ExpressionSyntax ExpressionParserDelegate(int level);

    /// <summary>
    /// Constructs an <see cref="ExpressionParserDelegate"/> for a set of prefix operators.
    /// </summary>
    /// <param name="operators">The set of prefix operators.</param>
    /// <returns>An <see cref="ExpressionParserDelegate"/> that recognizes <paramref name="operators"/> as prefix operators.</returns>
    private ExpressionParserDelegate Prefix(params TokenKind[] operators) => level =>
    {
        var opKind = this.PeekKind();
        if (operators.Contains(opKind))
        {
            // There is such operator on this level
            var op = this.Advance();

            this.CheckHeritageToken(op, "operator");

            var subexpr = this.ParseExpression(level);
            return new UnaryExpressionSyntax(op, subexpr);
        }
        else
        {
            // Just descent to next level
            return this.ParseExpression(level + 1);
        }
    };

    /// <summary>
    /// Constructs an <see cref="ExpressionParserDelegate"/> for a set of left-associative binary operators.
    /// </summary>
    /// <param name="operators">The set of binary operators.</param>
    /// <returns>An <see cref="ExpressionParserDelegate"/> that recognizes <paramref name="operators"/> as
    /// left-associative binary operators.</returns>
    private ExpressionParserDelegate BinaryLeft(params TokenKind[] operators) => level =>
    {
        // We unroll left-associativity into a loop
        var result = this.ParseExpression(level + 1);
        while (!this.CanBailOut(result))
        {
            var opKind = this.PeekKind();
            if (!operators.Contains(opKind)) break;
            var op = this.Advance();

            this.CheckHeritageToken(op, "operator");

            var right = this.ParseExpression(level + 1);
            result = new BinaryExpressionSyntax(result, op, right);
        }
        return result;
    };

    /// <summary>
    /// Constructs an <see cref="ExpressionParserDelegate"/> for a set of right-associative binary operators.
    /// </summary>
    /// <param name="operators">The set of binary operators.</param>
    /// <returns>An <see cref="ExpressionParserDelegate"/> that recognizes <paramref name="operators"/> as
    /// right-associative binary operators.</returns>
    private ExpressionParserDelegate BinaryRight(params TokenKind[] operators) => level =>
    {
        // Right-associativity is simply right-recursion
        var result = this.ParseExpression(level + 1);
        if (this.CanBailOut(result)) return result;
        var opKind = this.PeekKind();
        if (operators.Contains(opKind))
        {
            var op = this.Advance();
            var right = this.ParseExpression(level);
            result = new BinaryExpressionSyntax(result, op, right);
        }
        return result;
    };

    /// <summary>
    /// The list of all tokens that can start a declaration.
    /// </summary>
    private static readonly TokenKind[] declarationStarters =
    [
        TokenKind.KeywordImport,
        TokenKind.KeywordField,
        TokenKind.KeywordFunc,
        TokenKind.KeywordModule,
        TokenKind.KeywordVar,
        TokenKind.KeywordVal,
    ];

    /// <summary>
    /// The list of all tokens that can be a visibility modifier.
    /// </summary>
    private static readonly TokenKind[] visibilityModifiers =
    [
        TokenKind.KeywordInternal,
        TokenKind.KeywordPublic,
    ];

    /// <summary>
    /// The list of all tokens that can start an expression.
    /// </summary>
    private static readonly TokenKind[] expressionStarters =
    [
        TokenKind.Identifier,
        TokenKind.LiteralInteger,
        TokenKind.LiteralFloat,
        TokenKind.LiteralCharacter,
        TokenKind.LineStringStart,
        TokenKind.MultiLineStringStart,
        TokenKind.KeywordFalse,
        // NOTE: This is for later, when we decide if the lambda syntax should be func(...) = ...
        TokenKind.KeywordFunc,
        TokenKind.KeywordGoto,
        TokenKind.KeywordIf,
        TokenKind.KeywordNot,
        TokenKind.CNot,
        TokenKind.KeywordReturn,
        TokenKind.KeywordTrue,
        TokenKind.KeywordWhile,
        TokenKind.KeywordFor,
        TokenKind.ParenOpen,
        TokenKind.CurlyOpen,
        TokenKind.BracketOpen,
        TokenKind.Plus,
        TokenKind.Minus,
        TokenKind.Star,
    ];

    /// <summary>
    /// Checks if the token kind is visibility modifier.
    /// </summary>
    /// <param name="kind">The token kind to check for.</param>
    /// <returns>True, if the token kind is visibility modifier, otherwise false.</returns>
    private static bool IsVisibilityModifier(TokenKind kind) => visibilityModifiers.Contains(kind);

    /// <summary>
    /// Checks, if the current token kind and the potentially following tokens form an expression.
    /// </summary>
    /// <param name="kind">The current token kind.</param>
    /// <returns>True, uf <paramref name="kind"/> in the current state can form the start of an expression.</returns>
    private static bool IsExpressionStarter(TokenKind kind) => expressionStarters.Contains(kind);

    /// <summary>
    /// Checks, if there is a declaration starting at the given offset.
    /// </summary>
    /// <param name="context">The current context.</param>
    /// <param name="offset">The offset to check for.</param>
    /// <returns>True, if there is a declaration starting at the given offset, otherwise false.</returns>
    private bool IsDeclarationStarter(DeclarationContext context, int offset = 0)
    {
        var peek = this.Peek(offset);
        if (declarationStarters.Contains(peek.Kind)) return true;
        // Attribute
        if (peek.Kind == TokenKind.AtSign) return true;
        // Visibility modifier
        if (IsVisibilityModifier(peek.Kind)) return true;
        // Label
        if (context == DeclarationContext.Local
         && peek.Kind == TokenKind.Identifier
         && !this.CanBailOut(peek)
         && this.PeekKind(offset + 1) == TokenKind.Colon)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Parses a <see cref="CompilationUnitSyntax"/> until the end of input.
    /// </summary>
    /// <returns>The parsed <see cref="CompilationUnitSyntax"/>.</returns>
    public CompilationUnitSyntax ParseCompilationUnit()
    {
        var decls = SyntaxList.CreateBuilder<DeclarationSyntax>();
        while (this.PeekKind() != TokenKind.EndOfInput) decls.Add(this.ParseDeclaration());
        var end = this.Expect(TokenKind.EndOfInput);
        return new(decls.ToSyntaxList(), end);
    }

    /// <summary>
    /// Parses a script entry.
    /// </summary>
    /// <returns>The parsed <see cref="ScriptEntrySyntax"/>.</returns>
    public ScriptEntrySyntax ParseScriptEntry()
    {
        var statements = SyntaxList.CreateBuilder<StatementSyntax>();
        var value = null as ExpressionSyntax;

        while (this.PeekKind() != TokenKind.EndOfInput)
        {
            var element = this.ParseReplEntryElement();
            if (element is StatementSyntax stmt)
            {
                statements.Add(stmt);
            }
            else if (element is DeclarationSyntax decl)
            {
                statements.Add(new DeclarationStatementSyntax(decl));
            }
            else if (element is ExpressionSyntax expr)
            {
                value = expr;
                break;
            }
            else
            {
                throw new InvalidOperationException("illegal script entry parsed");
            }
        }

        var end = this.Expect(TokenKind.EndOfInput);
        return new ScriptEntrySyntax(statements.ToSyntaxList(), value, end);
    }

    private SyntaxNode ParseReplEntryElement()
    {
        var visibility = this.ParseVisibilityModifier();
        if (visibility is not null)
        {
            // Must be a declaration
            return this.ParseDeclaration();
        }

        if (this.IsDeclarationStarter(DeclarationContext.Local))
        {
            // Must be a declaration
            return this.ParseDeclaration();
        }

        // Either an expression or a statement
        // We can start by parsing an expression
        var expr = this.ParseExpression();

        // If there is a newline in the last token of the expression, we can assume it's an expression
        if (this.CanBailOut(expr)) return expr;

        // We can peek for a semicolon
        if (this.Matches(TokenKind.Semicolon, out var semicolon))
        {
            // It's a statement
            return new ExpressionStatementSyntax(expr, semicolon);
        }

        // It's an expression
        return expr;
    }

    /// <summary>
    /// Parses a global-level declaration.
    /// </summary>
    /// <param name="local">True, if the declaration should allow local context elements.</param>
    /// <returns>The parsed <see cref="DeclarationSyntax"/>.</returns>
    internal DeclarationSyntax ParseDeclaration(bool local = false) =>
        this.ParseDeclaration(local ? DeclarationContext.Local : DeclarationContext.Global);

    /// <summary>
    /// Parses a declaration.
    /// </summary>
    /// <param name="context">The current context.</param>
    /// <returns>The parsed <see cref="DeclarationSyntax"/>.</returns>
    private DeclarationSyntax ParseDeclaration(DeclarationContext context)
    {
        var attributes = this.ParseAttributeList();
        var visibility = this.ParseVisibilityModifier();
        switch (this.PeekKind())
        {
        case TokenKind.KeywordImport:
            return this.ParseImportDeclaration(attributes, visibility);

        case TokenKind.KeywordFunc:
            return this.ParseFunctionDeclaration(attributes, visibility, context);

        case TokenKind.KeywordModule:
            return this.ParseModuleDeclaration(attributes, visibility, context);

        case TokenKind.KeywordVar:
        case TokenKind.KeywordVal:
        case TokenKind.KeywordField:
            return this.ParseVariableDeclaration(attributes, visibility, context);

        case TokenKind.Identifier when this.PeekKind(1) == TokenKind.Colon:
            return this.ParseLabelDeclaration(attributes, visibility, context);

        default:
        {
            var input = this.Synchronize(t => t switch
            {
                _ when this.IsDeclarationStarter(context) => false,
                _ => true,
            });
            var info = DiagnosticInfo.Create(SyntaxErrors.UnexpectedInput, formatArgs: "declaration");
            var node = new UnexpectedDeclarationSyntax(attributes, visibility, input);
            this.AddDiagnostic(node, info);
            return node;
        }
        }
    }

    /// <summary>
    /// Parses a list of attributes.
    /// </summary>
    /// <returns>The parsed <see cref="SyntaxList{AttributeSyntax}"/>.</returns>
    private SyntaxList<AttributeSyntax>? ParseAttributeList()
    {
        if (this.PeekKind() != TokenKind.AtSign) return null;

        var result = SyntaxList.CreateBuilder<AttributeSyntax>();
        while (this.PeekKind() == TokenKind.AtSign)
        {
            result.Add(this.ParseAttribute());
        }
        return result.ToSyntaxList();
    }

    /// <summary>
    /// Parses a single attribute.
    /// </summary>
    /// <returns>The parsed <see cref="AttributeSyntax"/>.</returns>
    private AttributeSyntax ParseAttribute()
    {
        var atSign = this.Expect(TokenKind.AtSign);
        var type = this.ParseType();
        var args = null as ArgumentListSyntax;
        if (this.Matches(TokenKind.ParenOpen, out var openParen))
        {
            var argList = this.ParseSeparatedSyntaxList(
                elementParser: this.ParseExpression,
                separatorKind: TokenKind.Comma,
                stopKind: TokenKind.ParenClose);
            var closeParen = this.Expect(TokenKind.ParenClose);
            args = new ArgumentListSyntax(openParen, argList, closeParen);
        }
        return new AttributeSyntax(atSign, type, args);
    }

    /// <summary>
    /// Parses a statement.
    /// </summary>
    /// <returns>The parsed <see cref="StatementSyntax"/>.</returns>
    internal StatementSyntax ParseStatement(bool allowDecl)
    {
        switch (this.PeekKind())
        {
        // Declarations
        case TokenKind when allowDecl && this.IsDeclarationStarter(DeclarationContext.Local):
        {
            var decl = this.ParseDeclaration(DeclarationContext.Local);
            return new DeclarationStatementSyntax(decl);
        }

        // Expressions that can appear without braces
        case TokenKind.CurlyOpen:
        case TokenKind.KeywordIf:
        case TokenKind.KeywordWhile:
        case TokenKind.KeywordFor:
        {
            var expr = this.ParseControlFlowExpression(ControlFlowContext.Stmt);
            return new ExpressionStatementSyntax(expr, null);
        }

        // Assume expression
        default:
        {
            // TODO: This assumption might not be the best
            var expr = this.ParseExpression();
            var semicolon = this.Expect(TokenKind.Semicolon);
            return new ExpressionStatementSyntax(expr, semicolon);
        }
        }
    }

    /// <summary>
    /// Parses an <see cref="ImportDeclarationSyntax"/>.
    /// </summary>
    /// <param name="attributes">The attributes on the import.</param>
    /// <param name="visibility">The visibility modifier on the import.</param>
    /// <returns>The parsed <see cref="ImportDeclarationSyntax"/>.</returns>
    private ImportDeclarationSyntax ParseImportDeclaration(SyntaxList<AttributeSyntax>? attributes, SyntaxToken? visibility)
    {
        // There should not be attributes on import
        if (attributes is not null)
        {
            var info = DiagnosticInfo.Create(SyntaxErrors.UnexpectedAttributeList, formatArgs: "import");
            this.AddDiagnostic(attributes, info);
        }
        // There shouldn't be a modifier on import
        if (visibility is not null)
        {
            var info = DiagnosticInfo.Create(SyntaxErrors.UnexpectedVisibilityModifier, formatArgs: "declaration");
            this.AddDiagnostic(visibility, info);
        }
        // Import keyword
        var importKeyword = this.Expect(TokenKind.KeywordImport);
        // Path
        var path = this.ParseImportPath();
        // Ending semicolon
        var semicolon = this.Expect(TokenKind.Semicolon);
        return new ImportDeclarationSyntax(attributes, visibility, importKeyword, path, semicolon);
    }

    /// <summary>
    /// Parses an <see cref="ImportPathSyntax"/>.
    /// </summary>
    /// <returns>The parsed <see cref="ImportPathSyntax"/>.</returns>
    private ImportPathSyntax ParseImportPath()
    {
        // Root element
        var rootName = this.Expect(TokenKind.Identifier);
        var result = new RootImportPathSyntax(rootName) as ImportPathSyntax;
        // For every dot, we make a member-access
        while (this.Matches(TokenKind.Dot, out var dot))
        {
            var memberName = this.Expect(TokenKind.Identifier);
            result = new MemberImportPathSyntax(result, dot, memberName);
        }
        return result;
    }

    /// <summary>
    /// Parses a <see cref="VariableDeclarationSyntax"/>.
    /// </summary>
    /// <param name="attributes">The attributes on the import.</param>
    /// <param name="visibility">The visibility modifier on the import.</param>
    /// <param name="context">The current declaration context.</param>
    /// <returns>The parsed <see cref="VariableDeclarationSyntax"/>.</returns>
    private VariableDeclarationSyntax ParseVariableDeclaration(
        SyntaxList<AttributeSyntax>? attributes,
        SyntaxToken? visibility,
        DeclarationContext context)
    {
        if (context == DeclarationContext.Local && attributes is not null)
        {
            var info = DiagnosticInfo.Create(SyntaxErrors.UnexpectedAttributeList, formatArgs: "variable declaration");
            this.AddDiagnostic(attributes, info);
        }
        if (context == DeclarationContext.Local && visibility is not null)
        {
            var info = DiagnosticInfo.Create(SyntaxErrors.UnexpectedVisibilityModifier, formatArgs: "variable declaration");
            this.AddDiagnostic(visibility, info);
        }

        // Field modifier
        var fieldModifier = null as SyntaxToken;
        this.Matches(TokenKind.KeywordField, out fieldModifier);
        // var or val keyword
        var keyword = this.Expect([TokenKind.KeywordVal, TokenKind.KeywordVar], onError: TokenKind.KeywordVar);
        // Variable name
        var identifier = this.Expect(TokenKind.Identifier);
        // We don't necessarily have type specifier
        var type = null as TypeSpecifierSyntax;
        if (this.PeekKind() == TokenKind.Colon) type = this.ParseTypeSpecifier();
        // We don't necessarily have value assigned to the variable
        var assignment = null as ValueSpecifierSyntax;
        if (this.Matches(TokenKind.Assign, out var assign))
        {
            var value = this.ParseExpression();
            assignment = new(assign, value);
        }
        // Eat semicolon at the end of declaration
        var semicolon = this.Expect(TokenKind.Semicolon);
        return new VariableDeclarationSyntax(attributes, visibility, fieldModifier, keyword, identifier, type, assignment, semicolon);
    }

    /// <summary>
    /// Parses a function declaration.
    /// </summary>
    /// <param name="attributes">The attributes on the function.</param>
    /// <param name="visibility">The visibility modifier on the function.</param>
    /// <param name="context">The current declaration context.</param>
    /// <returns>The parsed <see cref="FunctionDeclarationSyntax"/>.</returns>
    private FunctionDeclarationSyntax ParseFunctionDeclaration(
        SyntaxList<AttributeSyntax>? attributes,
        SyntaxToken? visibility,
        DeclarationContext context)
    {
        if (context == DeclarationContext.Local && visibility is not null)
        {
            var info = DiagnosticInfo.Create(SyntaxErrors.UnexpectedVisibilityModifier, formatArgs: "function declaration");
            this.AddDiagnostic(visibility, info);
        }

        // Func keyword and name of the function
        var funcKeyword = this.Expect(TokenKind.KeywordFunc);
        var name = this.Expect(TokenKind.Identifier);

        // Optional generics
        var generics = null as GenericParameterListSyntax;
        if (this.PeekKind() == TokenKind.LessThan) generics = this.ParseGenericParameterList();

        // Parameters
        var openParen = this.Expect(TokenKind.ParenOpen);
        var funcParameters = this.ParseSeparatedSyntaxList(
            elementParser: this.ParseParameter,
            separatorKind: TokenKind.Comma,
            stopKind: TokenKind.ParenClose);
        var closeParen = this.Expect(TokenKind.ParenClose);

        // We don't necessarily have type specifier
        TypeSpecifierSyntax? returnType = null;
        if (this.PeekKind() == TokenKind.Colon) returnType = this.ParseTypeSpecifier();

        var body = this.ParseFunctionBody();

        return new FunctionDeclarationSyntax(
            attributes,
            visibility,
            funcKeyword,
            name,
            generics,
            openParen,
            funcParameters,
            closeParen,
            returnType,
            body);
    }

    /// <summary>
    /// Parses a module declaration.
    /// </summary>
    /// <param name="attributes">The attributes on the module.</param>
    /// <param name="visibility">The visibility modifier on the module.</param>
    /// <param name="context">The current declaration context.</param>
    /// <returns>The parsed <see cref="DeclarationSyntax"/>.</returns>
    private DeclarationSyntax ParseModuleDeclaration(
        SyntaxList<AttributeSyntax>? attributes,
        SyntaxToken? visibility,
        DeclarationContext context)
    {
        if (visibility is not null)
        {
            var info = DiagnosticInfo.Create(SyntaxErrors.UnexpectedVisibilityModifier, formatArgs: "module");
            this.AddDiagnostic(visibility, info);
        }

        // Module keyword and name of the module
        var moduleKeyword = this.Expect(TokenKind.KeywordModule);
        var name = this.Expect(TokenKind.Identifier);

        var openCurly = this.Expect(TokenKind.CurlyOpen);
        var decls = SyntaxList.CreateBuilder<DeclarationSyntax>();
        while (true)
        {
            switch (this.PeekKind())
            {
            case TokenKind.EndOfInput:
            case TokenKind.CurlyClose:
                // On a close curly or end of input, we can immediately exit
                goto end_of_block;
            default:
                decls.Add(this.ParseDeclaration(DeclarationContext.Global));
                break;
            }
        }
    end_of_block:
        var closeCurly = this.Expect(TokenKind.CurlyClose);

        var result = new ModuleDeclarationSyntax(
            attributes,
            visibility,
            moduleKeyword,
            name,
            openCurly,
            decls.ToSyntaxList(),
            closeCurly) as DeclarationSyntax;

        if (context != DeclarationContext.Global)
        {
            // Create diagnostic
            var info = DiagnosticInfo.Create(SyntaxErrors.IllegalElementInContext, formatArgs: "module");
            // Wrap up the result in an error node
            // NOTE: Attributes and visibility are already attached to the module
            result = new UnexpectedDeclarationSyntax(null, null, SyntaxList.Create(result as SyntaxNode));
            // Add diagnostic
            this.AddDiagnostic(result, info);
        }
        return result;
    }

    /// <summary>
    /// Parses a label declaration.
    /// </summary>
    /// <param name="attributes">The attributes on the module.</param>
    /// <param name="visibility">The visibility modifier on the module.</param>
    /// <param name="context">The current declaration context.</param>
    /// <returns>The parsed <see cref="DeclarationSyntax"/>.</returns>
    private DeclarationSyntax ParseLabelDeclaration(
        SyntaxList<AttributeSyntax>? attributes,
        SyntaxToken? visibility,
        DeclarationContext context)
    {
        if (attributes is not null)
        {
            var info = DiagnosticInfo.Create(SyntaxErrors.UnexpectedAttributeList, formatArgs: "label");
            this.AddDiagnostic(attributes, info);
        }
        if (visibility is not null)
        {
            var info = DiagnosticInfo.Create(SyntaxErrors.UnexpectedVisibilityModifier, formatArgs: "label");
            this.AddDiagnostic(visibility, info);
        }

        var labelName = this.Expect(TokenKind.Identifier);
        var colon = this.Expect(TokenKind.Colon);
        var result = new LabelDeclarationSyntax(attributes, visibility, labelName, colon) as DeclarationSyntax;
        if (context != DeclarationContext.Local)
        {
            // Create diagnostic
            var info = DiagnosticInfo.Create(SyntaxErrors.IllegalElementInContext, formatArgs: "label");
            // Wrap up the result in an error node
            // NOTE: Attributes and visibility are already attached to the label
            result = new UnexpectedDeclarationSyntax(null, null, SyntaxList.Create(result as SyntaxNode));
            // Add diagnostic
            this.AddDiagnostic(result, info);
        }
        return result;
    }

    /// <summary>
    /// Parses a function parameter.
    /// </summary>
    /// <returns>The parsed <see cref="ParameterSyntax"/>.</returns>
    private ParameterSyntax ParseParameter()
    {
        var attributes = this.ParseAttributeList();
        this.Matches(TokenKind.Ellipsis, out var variadic);
        var name = this.Expect(TokenKind.Identifier);
        var colon = this.Expect(TokenKind.Colon);
        var type = this.ParseType();
        return new(attributes, variadic, name, colon, type);
    }

    /// <summary>
    /// Parses a generic parameter list.
    /// </summary>
    /// <returns>The parsed <see cref="GenericParameterListSyntax"/>.</returns>
    private GenericParameterListSyntax ParseGenericParameterList()
    {
        var openBracket = this.Expect(TokenKind.LessThan);
        var parameters = this.ParseSeparatedSyntaxList(
            elementParser: this.ParseGenericParameter,
            separatorKind: TokenKind.Comma,
            stopKind: TokenKind.GreaterThan);
        var closeBracket = this.Expect(TokenKind.GreaterThan);
        var result = new GenericParameterListSyntax(openBracket, parameters, closeBracket);
        if (!parameters.Any())
        {
            // We don't allow empty generic lists
            var info = DiagnosticInfo.Create(SyntaxErrors.EmptyGenericList, "parameter");
            this.AddDiagnostic(result, info);
        }
        return result;
    }

    /// <summary>
    /// Parses a single generic parameter in a generic parameter list.
    /// </summary>
    /// <returns>The parsed <see cref="GenericParameterSyntax"/>.</returns>
    private GenericParameterSyntax ParseGenericParameter()
    {
        var name = this.Expect(TokenKind.Identifier);
        return new(name);
    }

    /// <summary>
    /// Parses a function body.
    /// </summary>
    /// <returns>The parsed <see cref="FunctionBodySyntax"/>.</returns>
    private FunctionBodySyntax ParseFunctionBody()
    {
        if (this.Matches(TokenKind.Assign, out var assign))
        {
            var expr = this.ParseExpression();
            var semicolon = this.Expect(TokenKind.Semicolon);
            return new InlineFunctionBodySyntax(assign, expr, semicolon);
        }
        else if (this.PeekKind() == TokenKind.CurlyOpen)
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
            var input = this.Synchronize(t => t.Kind switch
            {
                TokenKind.Semicolon or TokenKind.CurlyClose => false,
                _ when this.IsDeclarationStarter(DeclarationContext.Global) => false,
                _ => true,
            });
            var info = DiagnosticInfo.Create(SyntaxErrors.UnexpectedInput, formatArgs: "function body");
            var node = new UnexpectedFunctionBodySyntax(input);
            this.AddDiagnostic(node, info);
            return node;
        }
    }

    /// <summary>
    /// Parses a type specifier.
    /// </summary>
    /// <returns>The parsed <see cref="TypeSpecifierSyntax"/>.</returns>
    private TypeSpecifierSyntax ParseTypeSpecifier()
    {
        var colon = this.Expect(TokenKind.Colon);
        var type = this.ParseType();
        return new(colon, type);
    }

    /// <summary>
    /// Parses a type expression.
    /// </summary>
    /// <returns>The parsed <see cref="TypeSyntax"/>.</returns>
    private TypeSyntax ParseType() => this.ParseGenericLevelType();

    /// <summary>
    /// Parses a type expression with potential postfix notations, like member access or generics.
    /// </summary>
    /// <returns>The parsed <see cref="TypeSyntax"/>.</returns>
    private TypeSyntax ParseGenericLevelType()
    {
        var result = this.ParseAtomType();
        while (true)
        {
            var peek = this.PeekKind();
            if (peek == TokenKind.Dot)
            {
                // Member access
                var dot = this.Advance();
                var member = this.Expect(TokenKind.Identifier);
                result = new MemberTypeSyntax(result, dot, member);
            }
            else if (peek == TokenKind.LessThan)
            {
                // Generic instantiation
                var (openBracket, args, closeBracket) = this.ParseGenericArgumentList();
                result = new GenericTypeSyntax(result, openBracket, args, closeBracket);
            }
            else
            {
                break;
            }
        }
        return result;
    }

    /// <summary>
    /// Parses an atomic type expression.
    /// </summary>
    /// <returns>The parsed <see cref="TypeSyntax"/>.</returns>
    private TypeSyntax ParseAtomType()
    {
        if (this.Matches(TokenKind.Identifier, out var typeName))
        {
            return new NameTypeSyntax(typeName);
        }
        else
        {
            var input = this.Synchronize(t => t.Kind switch
            {
                TokenKind.Semicolon or TokenKind.Comma
             or TokenKind.ParenClose or TokenKind.BracketClose
             or TokenKind.CurlyClose or TokenKind.InterpolationEnd
             or TokenKind.Assign => false,
                _ when IsExpressionStarter(t.Kind) => false,
                _ => true,
            });
            var info = DiagnosticInfo.Create(SyntaxErrors.UnexpectedInput, formatArgs: "type");
            var node = new UnexpectedTypeSyntax(input);
            this.AddDiagnostic(node, info);
            return node;
        }
    }

    /// <summary>
    /// Parses any kind of control-flow expression, like a block, if or while expression.
    /// </summary>
    /// <param name="ctx">The current context we are in.</param>
    /// <returns>The parsed <see cref="ExpressionSyntax"/>.</returns>
    private ExpressionSyntax ParseControlFlowExpression(ControlFlowContext ctx)
    {
        var peekKind = this.PeekKind();
        Debug.Assert(peekKind is TokenKind.CurlyOpen
                              or TokenKind.KeywordIf
                              or TokenKind.KeywordWhile
                              or TokenKind.KeywordFor);
        return peekKind switch
        {
            TokenKind.CurlyOpen => this.ParseBlockExpression(ctx),
            TokenKind.KeywordIf => this.ParseIfExpression(ctx),
            TokenKind.KeywordWhile => this.ParseWhileExpression(ctx),
            TokenKind.KeywordFor => this.ParseForExpression(ctx),
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
    private BlockExpressionSyntax ParseBlockExpression(ControlFlowContext ctx)
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
        var openBrace = this.Expect(TokenKind.CurlyOpen);
        var stmts = SyntaxList.CreateBuilder<StatementSyntax>();
        ExpressionSyntax? value = null;
        while (true)
        {
            switch (this.PeekKind())
            {
            case TokenKind.EndOfInput:
            case TokenKind.CurlyClose:
                // On a close curly or out of input, we can immediately exit
                goto end_of_block;

            case TokenKind when this.IsDeclarationStarter(DeclarationContext.Local):
            {
                var decl = this.ParseDeclaration(DeclarationContext.Local);
                stmts.Add(new DeclarationStatementSyntax(decl));
                break;
            }

            case TokenKind.CurlyOpen:
            case TokenKind.KeywordIf:
            case TokenKind.KeywordWhile:
            case TokenKind.KeywordFor:
            {
                var expr = this.ParseControlFlowExpression(ctx);
                if (ctx == ControlFlowContext.Expr && this.PeekKind() == TokenKind.CurlyClose)
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
                if (IsExpressionStarter(this.PeekKind()))
                {
                    // Some expression
                    var expr = this.ParseExpression();
                    if (ctx == ControlFlowContext.Stmt || this.PeekKind() != TokenKind.CurlyClose)
                    {
                        // Likely just a statement, can continue
                        var semicolon = this.Expect(TokenKind.Semicolon);
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
                    var input = this.Synchronize(t => t.Kind switch
                    {
                        TokenKind.CurlyClose => false,
                        _ when this.IsDeclarationStarter(DeclarationContext.Local) => false,
                        _ when IsExpressionStarter(t.Kind) => false,
                        _ => true,
                    });
                    var info = DiagnosticInfo.Create(SyntaxErrors.UnexpectedInput, formatArgs: "statement");
                    var errNode = new UnexpectedStatementSyntax(input);
                    this.AddDiagnostic(errNode, info);
                    stmts.Add(errNode);
                }
                break;
            }
            }
        }
    end_of_block:
        var closeBrace = this.Expect(TokenKind.CurlyClose);
        return new(openBrace, stmts.ToSyntaxList(), value, closeBrace);
    }

    /// <summary>
    /// Parses an if-expression.
    /// </summary>
    /// <param name="ctx">The current context we are in.</param>
    /// <returns>The parsed <see cref="IfExpressionSyntax"/>.</returns>
    private IfExpressionSyntax ParseIfExpression(ControlFlowContext ctx)
    {
        var ifKeyword = this.Expect(TokenKind.KeywordIf);
        var openParen = this.Expect(TokenKind.ParenOpen);
        var condition = this.ParseExpression();
        var closeParen = this.Expect(TokenKind.ParenClose);
        var thenBody = this.ParseControlFlowBody(ctx);

        ElseClauseSyntax? elsePart = null;
        if (this.Matches(TokenKind.KeywordElse, out var elseKeyword))
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
    private WhileExpressionSyntax ParseWhileExpression(ControlFlowContext ctx)
    {
        var whileKeyword = this.Expect(TokenKind.KeywordWhile);
        var openParen = this.Expect(TokenKind.ParenOpen);
        var condition = this.ParseExpression();
        var closeParen = this.Expect(TokenKind.ParenClose);
        var body = this.ParseControlFlowBody(ctx);
        return new(whileKeyword, openParen, condition, closeParen, body);
    }

    /// <summary>
    /// Parses a for-expression.
    /// </summary>
    /// <param name="ctx">The current context we are in.</param>
    /// <returns>The parsed <see cref="ForExpressionSyntax"/>.</returns>
    private ForExpressionSyntax ParseForExpression(ControlFlowContext ctx)
    {
        // for (i: T in seq) { ... }
        var forKeyword = this.Expect(TokenKind.KeywordFor);
        var openParen = this.Expect(TokenKind.ParenOpen);
        var iterator = this.Expect(TokenKind.Identifier);
        TypeSpecifierSyntax? elementType = null;
        if (this.PeekKind() == TokenKind.Colon) elementType = this.ParseTypeSpecifier();
        var inKeyword = this.Expect(TokenKind.KeywordIn);
        var sequence = this.ParseExpression();
        var closeParen = this.Expect(TokenKind.ParenClose);
        var body = this.ParseControlFlowBody(ctx);
        return new(forKeyword, openParen, iterator, elementType, inKeyword, sequence, closeParen, body);
    }

    /// <summary>
    /// Parses an expression.
    /// </summary>
    /// <returns>The parsed <see cref="ExpressionSyntax"/>.</returns>
    internal ExpressionSyntax ParseExpression() => this.ParseExpression(0);

    /// <summary>
    /// Parses an expression.
    /// </summary>
    /// <param name="level">The current precedence level.</param>
    /// <returns>The parsed <see cref="ExpressionSyntax"/>.</returns>
    private ExpressionSyntax ParseExpression(int level) => level switch
    {
        // Finally the pseudo-statement-like constructs
        0 => this.ParsePseudoStatementLevelExpression(level),
        // Then assignment and compound assignment, which are **RIGHT ASSOCIATIVE**
        1 => this.BinaryRight(
            TokenKind.Assign,
            TokenKind.PlusAssign, TokenKind.MinusAssign,
            TokenKind.StarAssign, TokenKind.SlashAssign)(level),
        // Then binary or
        2 => this.BinaryLeft(TokenKind.KeywordOr, TokenKind.COr)(level),
        // Then binary and
        3 => this.BinaryLeft(TokenKind.KeywordAnd, TokenKind.CAnd)(level),
        // Then unary not
        4 => this.Prefix(TokenKind.KeywordNot, TokenKind.CNot)(level),
        // Then relational operators
        5 => this.ParseRelationalLevelExpression(level),
        // Then binary +, -
        6 => this.BinaryLeft(TokenKind.Plus, TokenKind.Minus)(level),
        // Then binary *, /, mod, rem
        7 => this.BinaryLeft(TokenKind.Star, TokenKind.Slash, TokenKind.KeywordMod, TokenKind.CMod, TokenKind.KeywordRem)(level),
        // Then prefix unary + and -
        8 => this.Prefix(TokenKind.Plus, TokenKind.Minus)(level),
        // Then comes call, indexing and member access
        9 => this.ParseCallLevelExpression(level),
        // Max precedence is atom
        10 => this.ParseAtomExpression(level),
        _ => throw new ArgumentOutOfRangeException(nameof(level)),
    };

    // Plumbing code for precedence parsing

    private ExpressionSyntax ParsePseudoStatementLevelExpression(int level)
    {
        switch (this.PeekKind())
        {
        case TokenKind.KeywordReturn:
        {
            var returnKeyword = this.Advance();
            ExpressionSyntax? value = null;
            if (IsExpressionStarter(this.PeekKind())) value = this.ParseExpression();
            return new ReturnExpressionSyntax(returnKeyword, value);
        }
        case TokenKind.KeywordGoto:
        {
            var gotoKeyword = this.Advance();
            var labelName = this.Expect(TokenKind.Identifier);
            return new GotoExpressionSyntax(gotoKeyword, new NameLabelSyntax(labelName));
        }
        default:
            return this.ParseExpression(level + 1);
        }
    }

    private ExpressionSyntax ParseRelationalLevelExpression(int level)
    {
        var left = this.ParseExpression(level + 1);
        if (this.CanBailOut(left)) return left;

        var comparisons = SyntaxList.CreateBuilder<ComparisonElementSyntax>();
        while (true)
        {
            var opKind = this.PeekKind();
            if (!SyntaxFacts.IsRelationalOperator(opKind)) break;
            var op = this.Advance();
            var right = this.ParseExpression(level + 1);
            comparisons.Add(new(op, right));
            if (this.CanBailOut(right)) break;
        }
        return comparisons.Count == 0
            ? left
            : new RelationalExpressionSyntax(left, comparisons.ToSyntaxList());
    }

    private ExpressionSyntax ParseCallLevelExpression(int level)
    {
        var result = this.ParseExpression(level + 1);
        while (!this.CanBailOut(result))
        {
            var peek = this.PeekKind();
            if (peek == TokenKind.ParenOpen)
            {
                var openParen = this.Expect(TokenKind.ParenOpen);
                var args = this.ParseSeparatedSyntaxList(
                    elementParser: this.ParseExpression,
                    separatorKind: TokenKind.Comma,
                    stopKind: TokenKind.ParenClose);
                var closeParen = this.Expect(TokenKind.ParenClose);
                result = new CallExpressionSyntax(result, openParen, args, closeParen);
            }
            else if (peek == TokenKind.BracketOpen)
            {
                var openBracket = this.Expect(TokenKind.BracketOpen);
                var args = this.ParseSeparatedSyntaxList(
                    elementParser: this.ParseExpression,
                    separatorKind: TokenKind.Comma,
                    stopKind: TokenKind.BracketClose);
                var closeBracket = this.Expect(TokenKind.BracketClose);
                result = new IndexExpressionSyntax(result, openBracket, args, closeBracket);
            }
            else if (peek == TokenKind.LessThan
                  && CanBeGenericInstantiated(result)
                  && this.DisambiguateLessThan() == LessThanDisambiguation.Generics)
            {
                // Generic instantiation
                var (openBracket, args, closeBracket) = this.ParseGenericArgumentList();
                result = new GenericExpressionSyntax(result, openBracket, args, closeBracket);
            }
            else if (this.Matches(TokenKind.Dot, out var dot))
            {
                var name = this.Expect(TokenKind.Identifier);
                result = new MemberExpressionSyntax(result, dot, name);
            }
            else
            {
                break;
            }
        }
        return result;
    }

    private ExpressionSyntax ParseAtomExpression(int level)
    {
        switch (this.PeekKind())
        {
        case TokenKind.LiteralInteger:
        case TokenKind.LiteralFloat:
        case TokenKind.LiteralCharacter:
        {
            var value = this.Advance();
            return new LiteralExpressionSyntax(value);
        }
        case TokenKind.KeywordTrue:
        case TokenKind.KeywordFalse:
        {
            var value = this.Advance();
            return new LiteralExpressionSyntax(value);
        }
        case TokenKind.LineStringStart:
            return this.ParseLineString();
        case TokenKind.MultiLineStringStart:
            return this.ParseMultiLineString();
        case TokenKind.Identifier:
        {
            var name = this.Advance();
            return new NameExpressionSyntax(name);
        }
        case TokenKind.ParenOpen:
        {
            var openParen = this.Expect(TokenKind.ParenOpen);
            var expr = this.ParseExpression();
            var closeParen = this.Expect(TokenKind.ParenClose);
            return new GroupingExpressionSyntax(openParen, expr, closeParen);
        }
        case TokenKind.CurlyOpen:
        case TokenKind.KeywordIf:
        case TokenKind.KeywordWhile:
        case TokenKind.KeywordFor:
            return this.ParseControlFlowExpression(ControlFlowContext.Expr);
        default:
        {
            var input = this.Synchronize(t => t.Kind switch
            {
                TokenKind.Semicolon or TokenKind.Comma
             or TokenKind.ParenClose or TokenKind.BracketClose
             or TokenKind.CurlyClose or TokenKind.InterpolationEnd => false,
                var kind when IsExpressionStarter(kind) => false,
                _ => true,
            });
            var info = DiagnosticInfo.Create(SyntaxErrors.UnexpectedInput, formatArgs: "expression");
            var node = new UnexpectedExpressionSyntax(input);
            this.AddDiagnostic(node, info);
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
        var openQuote = this.Expect(TokenKind.LineStringStart);
        var content = SyntaxList.CreateBuilder<StringPartSyntax>();
        while (true)
        {
            var peek = this.PeekKind();
            if (peek == TokenKind.StringContent || peek == TokenKind.EscapeSequence)
            {
                var part = this.Advance();
                content.Add(new TextStringPartSyntax(part));
            }
            else if (peek == TokenKind.InterpolationStart)
            {
                var start = this.Advance();
                var expr = this.ParseExpression();
                var end = this.Expect(TokenKind.InterpolationEnd);
                content.Add(new InterpolationStringPartSyntax(start, expr, end));
            }
            else
            {
                // We need a close quote for line strings then
                break;
            }
        }
        var closeQuote = this.Expect(TokenKind.LineStringEnd);
        return new(openQuote, content.ToSyntaxList(), closeQuote);
    }

    /// <summary>
    /// Parses a multi-line string expression.
    /// </summary>
    /// <returns>The parsed <see cref="StringExpressionSyntax"/>.</returns>
    private StringExpressionSyntax ParseMultiLineString()
    {
        var openQuote = this.Expect(TokenKind.MultiLineStringStart);
        var content = SyntaxList.CreateBuilder<StringPartSyntax>();
        // We check if there's a newline
        if (!openQuote.TrailingTrivia.Any(t => t.Kind == TriviaKind.Newline))
        {
            // Possible stray tokens inline
            var input = this.Synchronize(t => t.Kind switch
            {
                TokenKind.MultiLineStringEnd or TokenKind.StringNewline => false,
                _ => true,
            });
            var info = DiagnosticInfo.Create(SyntaxErrors.ExtraTokensInlineWithOpenQuotesOfMultiLineString);
            var unexpected = new UnexpectedStringPartSyntax(input);
            this.AddDiagnostic(unexpected, info);
            content.Add(unexpected);
        }
        while (true)
        {
            var peek = this.PeekKind();
            if (peek == TokenKind.StringContent || peek == TokenKind.StringNewline || peek == TokenKind.EscapeSequence)
            {
                var part = this.Advance();
                content.Add(new TextStringPartSyntax(part));
            }
            else if (peek == TokenKind.InterpolationStart)
            {
                var start = this.Advance();
                var expr = this.ParseExpression();
                var end = this.Expect(TokenKind.InterpolationEnd);
                content.Add(new InterpolationStringPartSyntax(start, expr, end));
            }
            else
            {
                // We need a close quote for line strings then
                break;
            }
        }
        var closeQuote = this.Expect(TokenKind.MultiLineStringEnd);
        // We need to check if the close quote is on a newline
        // There are 2 cases:
        //  - the leading trivia of the closing quotes contains a newline
        //  - the string is empty and the opening quotes trailing trivia contains a newline
        var isClosingQuoteOnNewline =
               closeQuote.LeadingTrivia.Any(t => t.Kind == TriviaKind.Newline)
            || (content.Count == 0 && openQuote.TrailingTrivia.Any(t => t.Kind == TriviaKind.Newline));
        if (isClosingQuoteOnNewline)
        {
            Debug.Assert(closeQuote.LeadingTrivia.Count <= 2);
            Debug.Assert(openQuote.TrailingTrivia.Any(t => t.Kind == TriviaKind.Newline)
                      || closeQuote.LeadingTrivia.Any(t => t.Kind == TriviaKind.Newline));
            if (closeQuote.LeadingTrivia.Count == 2)
            {
                // The first trivia was newline, the second must be spaces
                Debug.Assert(closeQuote.LeadingTrivia[1].Kind == TriviaKind.Whitespace);
                // We take the whitespace text and check if every line in the string obeys that as a prefix
                var prefix = closeQuote.LeadingTrivia[1].Text;
                var nextIsNewline = true;
                foreach (var part in content)
                {
                    if (part is TextStringPartSyntax textPart)
                    {
                        if (textPart.Content.Kind == TokenKind.StringNewline)
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
                            var info = DiagnosticInfo.Create(SyntaxErrors.InsufficientIndentationInMultiLinString);
                            var diag = new SyntaxDiagnosticInfo(info, Offset: 0, Width: whitespaceLength);
                            this.AddDiagnostic(textPart, diag);
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
            var info = DiagnosticInfo.Create(SyntaxErrors.ClosingQuotesOfMultiLineStringNotOnNewLine);
            this.AddDiagnostic(closeQuote, info);
        }
        return new(openQuote, content.ToSyntaxList(), closeQuote);
    }

    /// <summary>
    /// Parses a generic argument list and checks for empty list to report as an error, if needed.
    /// </summary>
    /// <returns>The parsed <see cref="ParsedGenericArgumentList"/>.</returns>
    private ParsedGenericArgumentList ParseGenericArgumentList()
    {
        var openBracket = this.Expect(TokenKind.LessThan);
        var args = this.ParseSeparatedSyntaxList(
            elementParser: this.ParseType,
            separatorKind: TokenKind.Comma,
            stopKind: TokenKind.GreaterThan);
        var closeBracket = this.Expect(TokenKind.GreaterThan);
        if (!args.Any())
        {
            var info = DiagnosticInfo.Create(SyntaxErrors.EmptyGenericList, "argument");
            // NOTE: We add the diagnostic to the closing bracket, it's more logical because it's the 'early terminator' token
            this.AddDiagnostic(closeBracket, info);
        }
        return new(openBracket, args, closeBracket);
    }

    /// <summary>
    /// Checks, if a given syntax can be followed by a generic argument list.
    /// </summary>
    /// <param name="syntaxNode">The node to check.</param>
    /// <returns>True, if <paramref name="syntaxNode"/> can be followed by a generic argument list.</returns>
    private static bool CanBeGenericInstantiated(SyntaxNode syntaxNode) => syntaxNode
        is NameExpressionSyntax
        or NameTypeSyntax
        or MemberExpressionSyntax
        or MemberTypeSyntax;

    /// <summary>
    /// Attempts to disambiguate the upcoming less-than token.
    /// </summary>
    /// <returns>The result of the disambiguation.</returns>
    private LessThanDisambiguation DisambiguateLessThan()
    {
        var offset = 0;
        return this.DisambiguateLessThan(ref offset);
    }

    /// <summary>
    /// Attempts to disambiguate the upcoming less-than token.
    /// </summary>
    /// <param name="offset">The offset to start disambiguation from. The value will be updated to the farthest
    /// offset that was peeked to disambiguate. If the token turns out to be a generic argument list, it is set
    /// to the offset after the matching '>'.</param>
    /// <returns>The result of the disambiguation.</returns>
    private LessThanDisambiguation DisambiguateLessThan(ref int offset)
    {
        Debug.Assert(this.PeekKind(offset) == TokenKind.LessThan);

        // Skip '<'
        ++offset;
        while (true)
        {
            var peek = this.PeekKind(offset);

            switch (peek)
            {
            case TokenKind.Dot:
            case TokenKind.Comma:
            {
                // Just skip, legal here
                ++offset;
                break;
            }
            case TokenKind.Identifier:
            {
                // Special case, we are in REPL mode
                // and are past an identifier
                // If we can bail, do so to avoid overpeeking
                if (this.CanBailOut(this.Peek(offset))) return LessThanDisambiguation.Operator;

                ++offset;
                // We can have a nested generic here
                if (this.PeekKind(offset) == TokenKind.LessThan)
                {
                    // Judge this list then
                    var judgement = this.DisambiguateLessThan(ref offset);
                    // If a nested thing is not a generic list, we are not either
                    if (judgement == LessThanDisambiguation.Operator) return LessThanDisambiguation.Operator;
                    // Otherwise, it's still fair game to be both
                }
                break;
            }
            case TokenKind.GreaterThan:
            {
                // We could not decide, we peek one ahead to determine
                ++offset;
                var next = this.PeekKind(offset);
                return next switch
                {
                    TokenKind.ParenOpen => LessThanDisambiguation.Generics,
                    _ when IsExpressionStarter(next) => LessThanDisambiguation.Operator,
                    _ => LessThanDisambiguation.Generics,
                };
            }
            default:
                // Illegal in generics
                return LessThanDisambiguation.Operator;
            }
        }
    }

    // General utilities

    /// <summary>
    /// Parses a <see cref="SeparatedSyntaxList{TNode}"/>.
    /// </summary>
    /// <typeparam name="TNode">The element type of the list.</typeparam>
    /// <param name="elementParser">The parser function that parses a single element.</param>
    /// <param name="separatorKind">The kind of the separator token.</param>
    /// <param name="stopKind">The kind of the token that definitely ends this construct.</param>
    /// <returns>The parsed <see cref="SeparatedSyntaxList{TNode}"/>.</returns>
    private SeparatedSyntaxList<TNode> ParseSeparatedSyntaxList<TNode>(
        Func<TNode> elementParser,
        TokenKind separatorKind,
        TokenKind stopKind)
        where TNode : SyntaxNode
    {
        var elements = SeparatedSyntaxList.CreateBuilder<TNode>();
        while (true)
        {
            // Stop token met, don't go further
            if (this.PeekKind() == stopKind) break;
            // Parse an element
            var element = elementParser();
            elements.Add(element);
            // If the next token is not a punctuation, we are done
            if (!this.Matches(separatorKind, out var punct)) break;
            // We had a punctuation, we can continue
            elements.Add(punct);
        }
        return elements.ToSeparatedSyntaxList();
    }

    private SyntaxToken? ParseVisibilityModifier() => IsVisibilityModifier(this.PeekKind()) ? this.Advance() : null;

    private bool CanBailOut(SyntaxNode node)
    {
        if (parserMode != ParserMode.Repl) return false;
        return node.LastToken?.TrailingTrivia.Any(t => t.Kind == TriviaKind.Newline) == true;
    }

    /// <summary>
    /// Checks a <see cref="SyntaxToken"/> for whether it is a heritage token and reports an error in case it is.
    /// </summary>
    /// <param name="token">The token to check.</param>
    /// <param name="syntaxKind">The text which is displayed in the reported diagnostic indicating what kind of syntactic element the heritage token is.</param>
    private void CheckHeritageToken(SyntaxToken token, string syntaxKind)
    {
        if (SyntaxFacts.GetHeritageReplacement(token.Kind) is not { } replacementKind) return;

        var info = DiagnosticInfo.Create(SyntaxErrors.CHeritageToken, SyntaxFacts.GetUserFriendlyName(token.Kind), syntaxKind, SyntaxFacts.GetUserFriendlyName(replacementKind));
        this.AddDiagnostic(token, info);
    }

    // Token-level operators

    /// <summary>
    /// Performs synchronization, meaning it consumes <see cref="SyntaxToken"/>s from the input
    /// while a given condition is met.
    /// </summary>
    /// <param name="keepGoing">The predicate that dictates if the consumption should keep going.</param>
    /// <returns>The consumed list of <see cref="SyntaxToken"/>s as <see cref="SyntaxNode"/>s.</returns>
    private SyntaxList<SyntaxNode> Synchronize(Func<SyntaxToken, bool> keepGoing)
    {
        // NOTE: A possible improvement could be to track opening and closing token pairs optionally
        var input = SyntaxList.CreateBuilder<SyntaxNode>();
        while (true)
        {
            var peek = this.Peek();
            if (peek.Kind == TokenKind.EndOfInput) break;
            if (!keepGoing(peek)) break;
            input.Add(this.Advance());
        }
        return input.ToSyntaxList();
    }

    /// <summary>
    /// Expects a certain kind of token to be at the current position.
    /// If it is, the token is consumed.
    /// </summary>
    /// <param name="kind">The expected <see cref="TokenKind"/>.</param>
    /// <returns>The consumed <see cref="SyntaxToken"/>.</returns>
    private SyntaxToken Expect(TokenKind kind) => this.Expect([kind], kind);

    /// <summary>
    /// Expects certain allowed tokens to be at the current position.
    /// If the upcoming token is one of the allowed kinds, it is consumed.
    /// </summary>
    /// <param name="kinds">The expected <see cref="TokenKind"/>s.</param>
    /// <param name="onError">The <see cref="TokenKind"/> to construct in case of an error.</param>
    /// <returns>The consumed <see cref="SyntaxToken"/>.</returns>
    private SyntaxToken Expect(TokenKind[] kinds, TokenKind onError)
    {
        var peekKind = this.PeekKind();
        if (!kinds.Contains(peekKind))
        {
            // We construct an empty token that signals that this is missing from the tree
            // The attached diagnostic message describes what is missing
            var friendlyName = string.Join(" or ", kinds.Select(SyntaxFacts.GetUserFriendlyName));
            var info = DiagnosticInfo.Create(SyntaxErrors.ExpectedToken, formatArgs: friendlyName);
            var diag = new SyntaxDiagnosticInfo(info, Offset: 0, Width: 0);
            var errorToken = SyntaxToken.From(onError, string.Empty);
            this.AddDiagnostic(errorToken, diag);
            return errorToken;
        }
        return this.Advance();
    }

    /// <summary>
    /// Checks if the upcoming token has kind <paramref name="kind"/>.
    /// If it is, the token is consumed.
    /// </summary>
    /// <param name="kind">The <see cref="TokenKind"/> to match.</param>
    /// <param name="token">The matched token is written here.</param>
    /// <returns>True, if the upcoming token is of kind <paramref name="kind"/>.</returns>
    private bool Matches(TokenKind kind, [MaybeNullWhen(false)] out SyntaxToken token)
    {
        if (this.PeekKind() == kind)
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
    /// Peeks ahead a token in the token source.
    /// </summary>
    /// <param name="offset">The amount to peek ahead.</param>
    /// <returns>The <see cref="SyntaxToken"/> that is <paramref name="offset"/> ahead.</returns>
    private SyntaxToken Peek(int offset = 0) => tokenSource.Peek(offset);

    /// <summary>
    /// Peeks ahead for the kind of the token in the token source.
    /// </summary>
    /// <param name="offset">The amount to peek ahead.</param>
    /// <returns>The <see cref="TokenKind"/> of the token that is <paramref name="offset"/> ahead.</returns>
    private TokenKind PeekKind(int offset = 0) => this.Peek(offset).Kind;

    /// <summary>
    /// Advances the parser in the token source with one token.
    /// </summary>
    /// <returns>The consumed <see cref="SyntaxToken"/>.</returns>
    private SyntaxToken Advance()
    {
        var token = tokenSource.Peek();
        tokenSource.Advance();
        return token;
    }

    private void AddDiagnostic(SyntaxNode node, DiagnosticInfo info)
    {
        var diag = new SyntaxDiagnosticInfo(info, Offset: 0, Width: node.Width);
        this.AddDiagnostic(node, diag);
    }

    private void AddDiagnostic(SyntaxNode node, SyntaxDiagnosticInfo diagnostic) =>
        diagnostics.Add(node, diagnostic);
}
