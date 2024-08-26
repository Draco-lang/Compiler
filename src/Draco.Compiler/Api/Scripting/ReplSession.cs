using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Scripting;
using Draco.Compiler.Internal.Syntax;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using DeclarationSyntax = Draco.Compiler.Api.Syntax.DeclarationSyntax;
using ExpressionSyntax = Draco.Compiler.Api.Syntax.ExpressionSyntax;
using ScriptEntrySyntax = Draco.Compiler.Api.Syntax.ScriptEntrySyntax;
using StatementSyntax = Draco.Compiler.Api.Syntax.StatementSyntax;
using SyntaxNode = Draco.Compiler.Api.Syntax.SyntaxNode;

namespace Draco.Compiler.Api.Scripting;

/// <summary>
/// Represents an interactive REPL session.
/// </summary>
public sealed class ReplSession
{
    /// <summary>
    /// Checks if the given text is a complete entry to be parsed by the REPL.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True, if <paramref name="text"/> is a complete entry.</returns>
    public static bool IsCompleteEntry(string text)
    {
        var tree = SyntaxTree.ParseScript(SourceReader.From(text));
        return SyntaxFacts.IsCompleteEntry(tree.Root);
    }

    private readonly List<Script<object?>> previousEntries = [];
    private readonly ReplContext context = new();

    public ReplSession(ImmutableArray<MetadataReference> metadataReferences)
    {
        foreach (var reference in metadataReferences) this.context.AddMetadataReference(reference);
    }

    /// <summary>
    /// Adds global imports to the session.
    /// </summary>
    /// <param name="importPaths">The import paths to add.</param>
    public void AddImports(params string[] importPaths) =>
        this.AddImports(importPaths.AsEnumerable());

    /// <summary>
    /// Adds global imports to the session.
    /// </summary>
    /// <param name="importPaths">The import paths to add.</param>
    public void AddImports(IEnumerable<string> importPaths)
    {
        foreach (var path in importPaths) this.context.AddImport(path);
    }

    /// <summary>
    /// Evaluates the given source code.
    /// </summary>
    /// <param name="text">The source code to evaluate.</param>
    /// <returns>The execution result.</returns>
    public ExecutionResult<object?> Evaluate(string text) =>
        this.Evaluate<object?>(SourceReader.From(text));

    /// <summary>
    /// Evaluates the given source code.
    /// </summary>
    /// <param name="reader">The reader to read input from.</param>
    /// <returns>The execution result.</returns>
    public ExecutionResult<object?> Evaluate(TextReader reader) =>
        this.Evaluate<object?>(SourceReader.From(reader));

    /// <summary>
    /// Evaluates the given syntax node.
    /// </summary>
    /// <param name="node">The node to evaluate.</param>
    /// <returns>The execution result.</returns>
    public ExecutionResult<object?> Evaluate(SyntaxNode node) => this.Evaluate<object?>(node);

    /// <summary>
    /// Evaluates the given source code.
    /// </summary>
    /// <typeparam name="TResult">The result type expected.</typeparam>
    /// <param name="text">The source code to evaluate.</param>
    /// <returns>The execution result.</returns>
    public ExecutionResult<TResult> Evaluate<TResult>(string text) =>
        this.Evaluate<TResult>(SourceReader.From(text));

    /// <summary>
    /// Evaluates the given source code.
    /// </summary>
    /// <typeparam name="TResult">The result type expected.</typeparam>
    /// <param name="reader">The text reader to read input from.</param>
    /// <returns>The execution result.</returns>
    public ExecutionResult<TResult> Evaluate<TResult>(TextReader reader) =>
        this.Evaluate<TResult>(SourceReader.From(reader));

    /// <summary>
    /// Evaluates the given source code.
    /// </summary>
    /// <typeparam name="TResult">The result type expected.</typeparam>
    /// <param name="sourceReader">The source reader to read input from.</param>
    /// <returns>The execution result.</returns>
    internal ExecutionResult<TResult> Evaluate<TResult>(ISourceReader sourceReader)
    {
        var tree = SyntaxTree.ParseScript(sourceReader);

        // Check for syntax errors
        if (tree.HasErrors)
        {
            return ExecutionResult.Fail<TResult>(tree.Diagnostics.ToImmutableArray());
        }

        // Actually evaluate
        return this.Evaluate<TResult>(tree.Root);
    }

    /// <summary>
    /// Evaluates the given syntax node.
    /// </summary>
    /// <typeparam name="TResult">The result type expected.</typeparam>
    /// <param name="node">The node to evaluate.</param>
    /// <returns>The execution result.</returns>
    public ExecutionResult<TResult> Evaluate<TResult>(SyntaxNode node)
    {
        // Wrap in a tree
        var tree = ToSyntaxTree(node);

        // Create a script
        var script = this.MakeScript(tree);

        // Try to execute
        var result = script.Execute();

        // If failed, bail out
        if (!result.Success) return ExecutionResult.Fail<TResult>(result.Diagnostics);

        // Stash the entry
        this.previousEntries.Add(script);

        // We want to stash the exports of the script
        this.context.AddAll(script.GlobalImports);
        // And the metadata references
        this.context.AddMetadataReference(MetadataReference.FromAssembly(script.Assembly!));

        // Return result
        return ExecutionResult.Success((TResult)result.Value!);
    }

    private Script<object?> MakeScript(SyntaxTree tree) => Script.Create(
        syntaxTree: tree,
        globalImports: this.context.GlobalImports,
        metadataReferences: this.context.MetadataReferences,
        previousCompilation: this.previousEntries.Count == 0
            ? null
            : this.previousEntries[^1].Compilation,
        assemblyLoadContext: this.context.AssemblyLoadContext);

    private static SyntaxTree ToSyntaxTree(SyntaxNode node) => node switch
    {
        ScriptEntrySyntax se => SyntaxTree.Create(se),
        ExpressionSyntax e => SyntaxTree.Create(ScriptEntry(SyntaxList<StatementSyntax>(), e, EndOfInput)),
        StatementSyntax s => SyntaxTree.Create(ScriptEntry(SyntaxList(s), null, EndOfInput)),
        DeclarationSyntax d => SyntaxTree.Create(ScriptEntry(SyntaxList<StatementSyntax>(DeclarationStatement(d)), null, EndOfInput)),
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };
}
