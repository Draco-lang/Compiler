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

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : ILanguageServer
{
    public InitializeResult.ServerInfoResult Info => new()
    {
        Name = "Draco Language Server",
        Version = "0.1.0",
    };

    public IList<DocumentFilter> DocumentSelector => new[]
    {
        new DocumentFilter()
        {
            Language = "draco",
            Pattern = "**/*.draco",
        }
    };
    public TextDocumentSyncKind SyncKind => TextDocumentSyncKind.Full;

    private readonly ILanguageClient client;
    private readonly DracoConfigurationRepository configurationRepository;

    private Uri rootUri;
    private volatile Compilation compilation;

    private readonly CompletionService completionService;
    private readonly SignatureService signatureService;
    private readonly CodeFixService codeFixService;

    public DracoLanguageServer(ILanguageClient client)
    {
        this.client = client;
        this.configurationRepository = new(client);
        this.rootUri = default!; // Default value, it will be given correct value on initialization

        // Some empty defaults
        this.compilation = Compilation.Create(
            syntaxTrees: ImmutableArray<SyntaxTree>.Empty,
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        this.completionService = new CompletionService();
        this.completionService.AddProvider(new KeywordCompletionProvider());
        this.completionService.AddProvider(new ExpressionCompletionProvider());
        this.completionService.AddProvider(new MemberCompletionProvider());

        this.signatureService = new SignatureService();

        this.codeFixService = new CodeFixService();
        this.codeFixService.AddProvider(new ImportCodeFixProvider());
    }

    private void CreateCompilation()
    {
        var rootPath = this.rootUri.LocalPath;
        var syntaxTrees = Directory.GetFiles(rootPath, "*.draco", SearchOption.AllDirectories)
            .Select(x => SyntaxTree.Parse(SourceText.FromFile(x)))
            .ToImmutableArray();

        this.compilation = Compilation.Create(
            syntaxTrees: syntaxTrees,
            // NOTE: Temporary until we solve MSBuild communication
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
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

    public Task InitializeAsync(InitializeParams param)
    {
        var workspaceUri = ExtractRootUri(param);
        if (workspaceUri is null) return Task.CompletedTask;
        Volatile.Write(ref this.rootUri, workspaceUri);
        this.CreateCompilation();
        return Task.CompletedTask;
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
