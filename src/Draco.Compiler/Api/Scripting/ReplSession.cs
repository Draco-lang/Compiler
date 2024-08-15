using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using DeclarationSyntax = Draco.Compiler.Api.Syntax.DeclarationSyntax;
using ExpressionSyntax = Draco.Compiler.Api.Syntax.ExpressionSyntax;
using FunctionDeclarationSyntax = Draco.Compiler.Api.Syntax.FunctionDeclarationSyntax;
using ImportDeclarationSyntax = Draco.Compiler.Api.Syntax.ImportDeclarationSyntax;
using StatementSyntax = Draco.Compiler.Api.Syntax.StatementSyntax;
using SyntaxNode = Draco.Compiler.Api.Syntax.SyntaxNode;
using VariableDeclarationSyntax = Draco.Compiler.Api.Syntax.VariableDeclarationSyntax;

namespace Draco.Compiler.Api.Scripting;

public readonly record struct ReplResult(
    bool Success,
    object? Value,
    ImmutableArray<Diagnostic> Diagnostics);

public sealed class ReplSession
{
    private readonly record struct Context(Compilation Compilation, Assembly Assembly);

    private const string EvalFunctionName = ".eval";

    private readonly AssemblyLoadContext loadContext;
    private readonly Dictionary<string, Assembly> loadedAssemblies = [];
    private readonly List<Context> previousContexts = [];
    private readonly List<ImportDeclarationSyntax> importDeclarations = [];
    // TODO: Temporary, until we can inherit everything from the host
    private readonly ImmutableArray<MetadataReference> metadataReferences;

    public ReplSession(ImmutableArray<MetadataReference> metadataReferences)
    {
        this.loadContext = new AssemblyLoadContext("ReplSession", isCollectible: true);
        this.loadContext.Resolving += this.LoadContextResolving;
        this.metadataReferences = metadataReferences;
    }

    public ReplResult Evaluate(TextReader reader)
    {
        var syntaxDiagnostics = new SyntaxDiagnosticTable();

        // Construct a source reader
        var sourceReader = SourceReader.From(reader);
        // Construct a lexer
        var lexer = new Lexer(sourceReader, syntaxDiagnostics);
        // Construct a token source
        var tokenSource = TokenSource.From(lexer);
        // Construct a parser
        var parser = new Parser(tokenSource, syntaxDiagnostics, parserMode: ParserMode.Repl);
        // Parse a repl entry
        var node = parser.ParseReplEntry();
        // Make it into a tree
        var tree = SyntaxTree.Create(node);

        // Check for syntax errors
        if (syntaxDiagnostics.HasErrors)
        {
            var diagnostics = tree.Root
                .PreOrderTraverse()
                .SelectMany(syntaxDiagnostics.Get)
                .ToImmutableArray();
            return new(Success: false, Value: null, Diagnostics: diagnostics);
        }

        // Actually evaluate
        return this.Evaluate(tree.Root);
    }

    public ReplResult Evaluate(SyntaxNode node)
    {
        // Check for imports
        if (node is ImportDeclarationSyntax import)
        {
            this.importDeclarations.Add(import);
            return new(Success: true, Value: null, Diagnostics: []);
        }

        // Translate to a runnable function
        var decl = node switch
        {
            ExpressionSyntax expr => this.ToDeclaration(expr),
            DeclarationSyntax d => this.ToDeclaration(d),
            StatementSyntax stmt => this.ToDeclaration(stmt),
            _ => throw new ArgumentOutOfRangeException(nameof(node)),
        };

        // Wrap in a tree
        var tree = this.ToSyntaxTree(decl);

        // Make compilation
        var compilation = this.MakeCompilation(tree);

        // Emit the assembly
        var peStream = new MemoryStream();
        var result = compilation.Emit(peStream: peStream);

        // Check for errors
        if (!result.Success) return new(Success: false, Value: null, Diagnostics: result.Diagnostics);

        // We need to load the assembly in the current context
        var assembly = this.LoadAssembly(peStream);

        // Stash it for future use
        this.previousContexts.Add(new Context(Compilation: compilation, Assembly: assembly));

        // Retrieve the main module
        var mainModule = assembly.GetType(compilation.RootModulePath);
        Debug.Assert(mainModule is not null);

        // Run the eval function
        var eval = mainModule.GetMethod(EvalFunctionName);
        if (eval is not null)
        {
            var value = eval.Invoke(null, null);
            return new(
                Success: true,
                Value: value,
                Diagnostics: result.Diagnostics);
        }

        // This happens with declarations, nothing to run
        return new(
            Success: true,
            Value: null,
            Diagnostics: result.Diagnostics);
    }

    // public func .eval(): object = decl;
    private DeclarationSyntax ToDeclaration(ExpressionSyntax expr) => FunctionDeclaration(
        Visibility.Public,
        EvalFunctionName,
        ParameterList(),
        NameType("object"),
        InlineFunctionBody(expr));

    // public func .eval() = stmt;
    private DeclarationSyntax ToDeclaration(StatementSyntax stmt) => FunctionDeclaration(
        Visibility.Public,
        EvalFunctionName,
        ParameterList(),
        null,
        InlineFunctionBody(StatementExpression(stmt)));

    private SyntaxTree ToSyntaxTree(DeclarationSyntax decl) => SyntaxTree.Create(CompilationUnit(
        this.importDeclarations.Append(decl)));

    private DeclarationSyntax ToDeclaration(DeclarationSyntax decl)
    {
        // TODO: We can get rid of this if we make scoping smarter
        if (decl is VariableDeclarationSyntax varDecl)
        {
            decl = VariableDeclaration(
                Visibility.Public,
                varDecl.Name.Text,
                varDecl.Type?.Type,
                varDecl.Value?.Value);
        }
        else if (decl is FunctionDeclarationSyntax funcDecl)
        {
            decl = FunctionDeclaration(
                Visibility.Public,
                funcDecl.Name.Text,
                funcDecl.Generics?.Parameters,
                funcDecl.ParameterList,
                funcDecl.ReturnType?.Type,
                funcDecl.Body);
        }
        else
        {
            // TODO
            throw new NotImplementedException();
        }

        return decl;
    }

    private Compilation MakeCompilation(SyntaxTree tree) => Compilation.Create(
        syntaxTrees: [tree],
        metadataReferences: this.metadataReferences
            .Concat(this.previousContexts.Select(c => MetadataReference.FromAssembly(c.Assembly)))
            .ToImmutableArray(),
        rootModulePath: $"Context{this.previousContexts.Count}",
        assemblyName: $"ReplAssembly{this.previousContexts.Count}");

    private Assembly? LoadContextResolving(AssemblyLoadContext context, AssemblyName name)
    {
        if (name.Name is null) return null;
        return this.loadedAssemblies.TryGetValue(name.Name, out var assembly) ? assembly : null;
    }

    private Assembly LoadAssembly(MemoryStream peStream)
    {
        peStream.Position = 0;
        var assembly = this.loadContext.LoadFromStream(peStream);
        var assemblyName = assembly.GetName().Name;
        if (assemblyName is not null) this.loadedAssemblies.Add(assemblyName, assembly);
        return assembly;
    }
}
