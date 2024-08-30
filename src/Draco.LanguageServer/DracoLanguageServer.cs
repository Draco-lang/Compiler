using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.CodeCompletion;
using Draco.Compiler.Api.CodeFixes;
using Draco.Compiler.Api.Syntax;
using Draco.Lsp.Model;
using Draco.Lsp.Server;
using Draco.ProjectSystem;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : ILanguageServer
{
    public InitializeResult.ServerInfoResult Info => new()
    {
        Name = "Draco Language Server",
        Version = "0.1.0",
    };

    public IList<DocumentFilter> DocumentSelector =>
    [
        new DocumentFilter()
        {
            Language = "draco",
            Pattern = "**/*.draco",
        }
    ];
    public TextDocumentSyncKind SyncKind => TextDocumentSyncKind.Full;

    private readonly ILanguageClient client;
    private readonly DracoConfigurationRepository configurationRepository;

    private Uri rootUri;
    private volatile Compilation compilation;

    private readonly CompletionService completionService = CompletionService.CreateDefault();
    private readonly SignatureService signatureService = new();
    private readonly CodeFixService codeFixService = CodeFixService.CreateDefault();

    public DracoLanguageServer(ILanguageClient client)
    {
        this.client = client;
        this.configurationRepository = new(client);
        this.rootUri = default!; // Default value, it will be given correct value on initialization

        // Some empty defaults
        this.compilation = Compilation.Create(
            syntaxTrees: [],
            metadataReferences: []);
    }

    private async Task CreateCompilation()
    {
        var rootPath = this.rootUri.LocalPath;
        var workspace = Workspace.Initialize(rootPath);

        var projects = workspace.Projects.ToList();
        if (projects.Count == 0)
        {
            await this.client.ShowMessageAsync(MessageType.Error, "No project file found in the workspace.");
            return;
        }
        else if (projects.Count > 1)
        {
            await this.client.ShowMessageAsync(MessageType.Error, "Multiple project files found in the workspace.");
            return;
        }

        var project = projects[0];
        var buildResult = project.BuildDesignTime();

        if (!buildResult.Success)
        {
            await this.client.LogMessageAsync(MessageType.Error, buildResult.Log);
            await this.client.ShowMessageAsync(MessageType.Error, "Design-time build failed! See logs for details.");
            return;
        }

        var designTimeBuild = buildResult.Value;

        var syntaxTrees = Directory
            .GetFiles(rootPath, "*.draco", SearchOption.AllDirectories)
            .Select(x => SyntaxTree.Parse(SourceText.FromFile(x)))
            .ToImmutableArray();

        this.compilation = Compilation.Create(
            syntaxTrees: syntaxTrees,

            metadataReferences: designTimeBuild.References
                .Select(r => MetadataReference.FromFile(r.FullName))
                .ToImmutableArray(),
            rootModulePath: rootPath);
    }

    private static SyntaxTree? GetSyntaxTree(Compilation compilation, DocumentUri documentUri)
    {
        var uri = documentUri.ToUri();
        return compilation.SyntaxTrees.FirstOrDefault(t => t.SourceText.Path == uri);
    }

    private async Task UpdateDocument(DocumentUri documentUri, string? sourceText = null)
    {
        var compilation = this.compilation;

        var oldTree = GetSyntaxTree(compilation, documentUri);
        var newTree = SyntaxTree.Parse(sourceText is null
            ? SourceText.FromFile(documentUri.ToUri())
            : SourceText.FromText(documentUri.ToUri(), sourceText.AsMemory()));
        this.compilation = compilation.UpdateSyntaxTree(oldTree, newTree);

        await this.PublishDiagnosticsAsync(documentUri);
    }

    private async Task DeleteDocument(DocumentUri documentUri)
    {
        var compilation = this.compilation;

        var oldTree = GetSyntaxTree(compilation, documentUri);
        this.compilation = compilation.UpdateSyntaxTree(oldTree, null);
        await this.PublishDiagnosticsAsync(documentUri);
    }

    public void Dispose() { }

    public async Task InitializeAsync(InitializeParams param)
    {
        var workspaceUri = ExtractRootUri(param);
        if (workspaceUri is null) return;

        Volatile.Write(ref this.rootUri, workspaceUri);
        await this.CreateCompilation();
    }

    public async Task InitializedAsync(InitializedParams param)
    {
        await this.configurationRepository.UpdateConfigurationAsync();
    }

    public Task ShutdownAsync() => Task.CompletedTask;

    private static Uri? ExtractRootUri(InitializeParams param)
    {
        if (param.WorkspaceFolders is not null)
        {
            if (param.WorkspaceFolders.Count == 0) return null;
            return param.WorkspaceFolders[0].Uri;
        }
        // NOTE: VS still uses these...
#pragma warning disable CS0618 // Type or member is obsolete
        if (param.RootUri is not null) return param.RootUri.Value.ToUri();
        if (param.RootPath is not null) return new Uri(param.RootPath);
#pragma warning restore CS0618 // Type or member is obsolete
        return null;
    }
}
