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
using static Draco.Compiler.Api.Syntax.SyntaxFactory;

namespace Draco.Compiler.Api.Scripting;

public readonly record struct ReplResult(
    bool Success,
    object? Value,
    ImmutableArray<Diagnostic> Diagnostics);

public sealed class ReplSession
{
    private readonly record struct Context(Compilation Compilation, Assembly Assembly);

    private readonly AssemblyLoadContext loadContext;
    private readonly Dictionary<string, Assembly> loadedAssemblies = [];
    // TODO: Temporary, until we can inherit everything from the host
    private readonly ImmutableArray<MetadataReference> metadataReferences;
    private readonly List<Context> previousContexts = [];

    public ReplSession(ImmutableArray<MetadataReference> metadataReferences)
    {
        this.loadContext = new AssemblyLoadContext("ReplSession");
        this.loadContext.Resolving += this.LoadContextResolving;
        this.metadataReferences = metadataReferences;
    }

    public ReplResult Evaluate(SyntaxNode node)
    {
        // 1. In case it's an expression, we need to evaluate it
        // 2. In case it's a declaration, compile it and do nothing
        // 3. In case it's a statement, compile it and execute it

        var (compilation, mainModuleName) = node switch
        {
            ExpressionSyntax expr => this.CompileExpression(expr),
            DeclarationSyntax decl => this.CompileDeclaration(decl),
            StatementSyntax stmt => this.CompileStatement(stmt),
            _ => throw new ArgumentOutOfRangeException(nameof(node)),
        };

        var peStream = new MemoryStream();
        var result = compilation.Emit(peStream: peStream);

        if (!result.Success)
        {
            return new(
                Success: false,
                Value: null,
                Diagnostics: result.Diagnostics);
        }

        // We need to load the assembly in the current context
        var assembly = this.LoadAssembly(peStream);

        // Stash previous context
        this.previousContexts.Add(new Context(
            Compilation: compilation,
            Assembly: assembly));

        // Execute the code
        var mainModule = assembly.GetType(mainModuleName);
        Debug.Assert(mainModule is not null);

        var getValue = mainModule.GetMethod(".getvalue");
        if (getValue is not null)
        {
            var value = getValue.Invoke(null, null);
            return new(
                Success: true,
                Value: value,
                Diagnostics: result.Diagnostics);
        }

        var execute = mainModule.GetMethod(".execute");
        if (execute is not null)
        {
            _ = execute.Invoke(null, null);
            return new(
                Success: true,
                Value: null,
                Diagnostics: result.Diagnostics);
        }

        // This happens with declarations
        return new(
            Success: true,
            Value: null,
            Diagnostics: result.Diagnostics);
    }

    private (Compilation Compilation, string MainModuleName) CompileExpression(ExpressionSyntax expr)
    {
        // func .getvalue(): object = expr;

        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            Visibility.Public,
            ".getvalue",
            ParameterList(),
            NameType("object"),
            InlineFunctionBody(expr))));

        return this.MakeCompilation(tree);
    }

    private (Compilation Compilation, string MainModuleName) CompileDeclaration(DeclarationSyntax decl)
    {
        if (decl is VariableDeclarationSyntax varDecl)
        {
            decl = VariableDeclaration(
                Visibility.Public,
                varDecl.Name.Text,
                varDecl.Type?.Type,
                varDecl.Value?.Value);
        }
        else
        {
            // TODO
            throw new NotImplementedException();
        }

        var tree = SyntaxTree.Create(CompilationUnit(decl));

        return this.MakeCompilation(tree);
    }

    private (Compilation Compilation, string MainModuleName) CompileStatement(StatementSyntax stmt)
    {
        // func .execute() = stmt;

        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            Visibility.Public,
            ".execute",
            ParameterList(),
            null,
            InlineFunctionBody(StatementExpression(stmt)))));

        return this.MakeCompilation(tree);
    }

    private (Compilation Compilation, string MainModuleName) MakeCompilation(SyntaxTree tree)
    {
        var moduleName = $"Context{this.previousContexts.Count}";
        var assemblyName = $"ReplAssembly{this.previousContexts.Count}";

        var compilation = Compilation.Create(
            syntaxTrees: [tree],
            metadataReferences: this.metadataReferences
                .Concat(this.previousContexts.Select(c => MetadataReference.FromAssembly(c.Assembly)))
                .ToImmutableArray(),
            rootModulePath: moduleName,
            assemblyName: assemblyName);
        return (compilation, moduleName);
    }

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
