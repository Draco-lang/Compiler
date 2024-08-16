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
using Draco.Compiler.Internal.Scripting;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Syntax;
using Draco.Compiler.Internal.Utilities;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using DeclarationSyntax = Draco.Compiler.Api.Syntax.DeclarationSyntax;
using ExpressionSyntax = Draco.Compiler.Api.Syntax.ExpressionSyntax;
using FunctionDeclarationSyntax = Draco.Compiler.Api.Syntax.FunctionDeclarationSyntax;
using ImportDeclarationSyntax = Draco.Compiler.Api.Syntax.ImportDeclarationSyntax;
using StatementSyntax = Draco.Compiler.Api.Syntax.StatementSyntax;
using SyntaxNode = Draco.Compiler.Api.Syntax.SyntaxNode;
using VariableDeclarationSyntax = Draco.Compiler.Api.Syntax.VariableDeclarationSyntax;
using ImportPathSyntax = Draco.Compiler.Api.Syntax.ImportPathSyntax;
using RootImportPathSyntax = Draco.Compiler.Api.Syntax.RootImportPathSyntax;
using MemberImportPathSyntax = Draco.Compiler.Api.Syntax.MemberImportPathSyntax;

namespace Draco.Compiler.Api.Scripting;

public sealed class ReplSession
{
    private readonly record struct HistoryEntry(Compilation Compilation, Assembly Assembly);

    private const string EvalFunctionName = ".eval";

    private readonly AssemblyLoadContext loadContext;
    private readonly Dictionary<string, Assembly> loadedAssemblies = [];
    private readonly List<HistoryEntry> previousEntries = [];
    private readonly ReplContext context = new();
    // TODO: Temporary, until we can inherit everything from the host
    private readonly ImmutableArray<MetadataReference>.Builder metadataReferences;

    public ReplSession(ImmutableArray<MetadataReference> metadataReferences)
    {
        this.loadContext = new AssemblyLoadContext("ReplSession", isCollectible: true);
        this.loadContext.Resolving += this.LoadContextResolving;
        this.metadataReferences = metadataReferences.ToBuilder();
    }

    public ExecutionResult<object?> Evaluate(TextReader reader) => this.Evaluate<object?>(reader);

    public ExecutionResult<object?> Evaluate(SyntaxNode node) => this.Evaluate<object?>(node);

    public ExecutionResult<TResult> Evaluate<TResult>(TextReader reader)
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
            return ExecutionResult.Fail<TResult>(diagnostics);
        }

        // Actually evaluate
        return this.Evaluate<TResult>(tree.Root);
    }

    public ExecutionResult<TResult> Evaluate<TResult>(SyntaxNode node)
    {
        // Check for imports
        if (node is ImportDeclarationSyntax import)
        {
            this.context.AddImport(ExtractImportPath(import.Path));
            return ExecutionResult.Success(default(TResult)!);
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
        if (!result.Success) return ExecutionResult.Fail<TResult>(result.Diagnostics);

        // If it was a declaration, track it
        if (node is DeclarationSyntax)
        {
            var semanticModel = compilation.GetSemanticModel(tree);
            var decl2 = tree.Root
                .PreOrderTraverse()
                .OfType<DeclarationSyntax>()
                .First(d => d.GetType() == decl.GetType());
            var symbol = semanticModel.GetDeclaredSymbolInternal(decl2);
            if (symbol is not null) this.context.AddSymbol(symbol);
        }

        // We need to load the assembly in the current context
        var assembly = this.LoadAssembly(peStream);

        // Stash it for future use
        this.previousEntries.Add(new HistoryEntry(Compilation: compilation, Assembly: assembly));
        this.metadataReferences.Add(MetadataReference.FromAssembly(assembly));

        // Retrieve the main module
        var mainModule = assembly.GetType(compilation.RootModulePath);
        Debug.Assert(mainModule is not null);

        // Run the eval function
        var eval = mainModule.GetMethod(EvalFunctionName);
        if (eval is not null)
        {
            var value = (TResult?)eval.Invoke(null, null);
            return ExecutionResult.Success(value!, result.Diagnostics);
        }

        // This happens with declarations, nothing to run
        return ExecutionResult.Success(default(TResult)!, result.Diagnostics);
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

    private SyntaxTree ToSyntaxTree(DeclarationSyntax decl) => SyntaxTree.Create(CompilationUnit(decl));

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
        metadataReferences: this.metadataReferences.ToImmutableArray(),
        globalImports: this.context.GlobalImports,
        rootModulePath: $"Context{this.previousEntries.Count}",
        assemblyName: $"ReplAssembly{this.previousEntries.Count}",
        metadataAssemblies: this.previousEntries.Count == 0
            ? null
            : this.previousEntries[^1].Compilation.MetadataAssembliesDict);

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

    private static string ExtractImportPath(ImportPathSyntax path) => path switch
    {
        RootImportPathSyntax root => root.Name.Text,
        MemberImportPathSyntax member => $"{ExtractImportPath(member.Accessed)}.{member.Member.Text}",
        _ => throw new ArgumentOutOfRangeException(nameof(path)),
    };
}
